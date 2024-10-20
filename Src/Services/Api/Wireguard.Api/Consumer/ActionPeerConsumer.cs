using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Wireguard.Api.Consumer;

public class ActionPeerConsumer(IConfiguration configuration, ILogger<ActionPeerConsumer> logger)
    : IConsumer<ActionPeerEvent>
{
    public async Task Consume(ConsumeContext<ActionPeerEvent> context)
    {
        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = """
                        SELECT * FROM PEER
                        WHERE (
                            TotalReceivedVolume - COALESCE(TotalVolume, 0) > 0 
                            OR (
                                ExpireTime < EXTRACT(EPOCH FROM NOW())
                                AND Status IN ('active', 'disabled', 'onhold')
                            )
                        )
                        """;

            IEnumerable<Peer> peers = await connection.QueryAsync<Peer>(query, transaction: transaction);

            long currentEpochTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Create a list to store update commands
            var updateCommands = new StringBuilder();

            foreach (var peer in peers)
            {
                if (peer.Status == PeerStatus.onhold.ToString() && peer.TotalReceivedVolume != 0)
                {
                    peer.Status = PeerStatus.active.ToString();
                    peer.ExpireTime = currentEpochTime + peer.OnHoldExpireDurection;

                    updateCommands.AppendLine($@"
                        UPDATE PEER SET Status = '{PeerStatus.active}', StartTime = {currentEpochTime}, ExpireTime = {peer.ExpireTime}
                        WHERE PublicKey = '{peer.PublicKey}';
                    ");
                }

                if (peer.TotalReceivedVolume - peer.TotalVolume > 0 && peer.Status == "active")
                {
                    updateCommands.AppendLine($@"
                        UPDATE PEER SET Status = '{PeerStatus.limited}'
                        WHERE PublicKey = '{peer.PublicKey}';
                    ");
                }

                if (peer.ExpireTime < currentEpochTime && peer.Status != "onhold")
                {
                    updateCommands.AppendLine($@"
                        UPDATE PEER SET Status = '{PeerStatus.expired}'
                        WHERE PublicKey = '{peer.PublicKey}';
                    ");
                }
            }

            // Execute batch updates
            if (updateCommands.Length > 0)
            {
                await connection.ExecuteAsync(updateCommands.ToString(), transaction: transaction);
                await connection.CloseAsync();
                logger.LogInformation("Batch update executed successfully.");
            }

            // Commit the transaction
            await transaction.CommitAsync();
            logger.LogInformation("Transaction committed successfully.");
        }
        catch (Exception e)
        {
            // Rollback the transaction in case of an error
            await transaction.RollbackAsync();
            logger.LogError(e, "Exception occurred during transaction.");
        }
    }
}