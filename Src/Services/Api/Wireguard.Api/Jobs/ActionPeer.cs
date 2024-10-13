using System.Security.Cryptography.X509Certificates;
using Dapper;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Jobs;

public class ActionPeer : IJob
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActionPeer> _logger;

    public ActionPeer(IConfiguration configuration, ILogger<ActionPeer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("actionPeer executing");

        await using var connection =
            new NpgsqlConnection(_configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = """
                        SELECT * FROM PEER
                        WHERE (
                            TotalReceivedVolume - COALESCE(TotalVolume, 0) > 0 
                            OR (
                                ExpireTime < EXTRACT(EPOCH FROM NOW())
                                AND Status IN ('Active', 'Disabled', 'OnHold')
                            )
                        );
                        """;

            var command = """
                          UPDATE PEER SET Status = @Status
                          WHERE PublicKey = @PublicKey
                          """;

            IEnumerable<Peer> peers = await connection.QueryAsync<Peer>(query, transaction);

            _logger.LogInformation("peer count" + peers.Count());

            long currentEpochTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var peer in peers)
            {
                if (peer.Status == PeerStatus.OnHold.ToString() & peer.TotalReceivedVolume != 0)
                {
                    await connection.ExecuteAsync(command, new
                    {
                        Status = PeerStatus.Active.ToString(),
                        PublicKey = peer.PublicKey
                    });
                }

                if (peer.TotalReceivedVolume - peer.TotalVolume > 0)
                {
                    await connection.ExecuteAsync(command, new
                    {
                        Status = PeerStatus.Limited.ToString(),
                        PublicKey = peer.PublicKey
                    });
                }

                if (peer.OnHoldExpireDurection < currentEpochTime)
                {
                    await connection.ExecuteAsync(command, new
                    {
                        Status = PeerStatus.Expired.ToString(),
                        PublicKey = peer.PublicKey
                    });
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("thorw exception :" + e.Message);
            await transaction.RollbackAsync();
        }
    }
}