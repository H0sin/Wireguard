using System.Net;
using Dapper;
using Npgsql;
using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer : IJob
{
    private readonly IConfiguration _configuration;

    public SyncPeer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"SyncPeer starting...");

        var transferData = await WireguardHelpers.GetTransferData();

        await using var connection =
            new NpgsqlConnection(_configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();

        if (transferData != null && transferData.Count > 0)
        {
            string command = """
                             UPDATE PEER SET 
                                         DownloadVolume=@DownloadVolume,
                                         UploadVolume=@UploadVolume,
                                         TotalReceivedVolume=@TotalReceivedVolume
                                         WHERE PublicKey = @PublicKey
                             """;

            try
            {
                foreach (var transfer in transferData)
                {
                    Console.WriteLine(transfer.PeerPublicKey);
                    await connection.ExecuteAsync(command, new
                    {
                        DownloadVolume = transfer.ReceivedBytes,
                        UploadVolume = transfer.SentBytes,
                        TotalReceivedVolume = transfer.ReceivedBytes + transfer.SentBytes,
                        PublicKey = transfer.PeerPublicKey
                    }, transaction);
                }

                await transaction.CommitAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await transaction.RollbackAsync();
            }
        }

        await Task.CompletedTask;
    }
}