using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Consumer;

public class DeletePeerConsumer(IConfiguration configuration,ILogger<DeletePeerConsumer> logger) : IConsumer<DeletePeerEvent>
{
    public async Task Consume(ConsumeContext<DeletePeerEvent> context)
    {
        logger.LogInformation("delete peer job started.....");

        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        try
        {
            var query = """
                                SELECT 
                                    I.Name AS InterfaceName,
                                    P.PublicKey,
                                    P.Status
                                    FROM Interface I
                                         JOIN Peer P ON I.Id = P.InterfaceId
                                WHERE P.Status IN ('expired', 'limited','disabled')
                        """;

            var peers = await connection.QueryAsync<PeerDto>(query, transaction);

            foreach (var peer in peers)
            {
                await WireguardHelpers.RemovePeer(peer.InterfaceName, peer.PublicKey);
                await WireguardHelpers.Save(peer.InterfaceName);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}