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
        var transferData = await WireguardHelpers.GetTransferDataAsync();

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
                Console.WriteLine($"{transferData.Count} peer transfers from {transferData.Count} files.");
                
                foreach (var transfer in transferData)
                {
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
                Console.WriteLine("thorw exception :" + e.Message);
                await transaction.RollbackAsync();
            }
        }
        else
        {
            Console.WriteLine("no data found");
        }

        await Task.CompletedTask;
    }
}