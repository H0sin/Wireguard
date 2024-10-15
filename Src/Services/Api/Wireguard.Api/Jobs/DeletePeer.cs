using Dapper;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class DeletePeer : IJob
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ActionPeer> _logger;

    public DeletePeer(IConfiguration configuration, ILogger<ActionPeer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("delete peer job started.....");

        await using var connection =
            new NpgsqlConnection(_configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

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