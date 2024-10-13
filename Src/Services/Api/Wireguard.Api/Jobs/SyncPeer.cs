using System.Net;
using Dapper;
using Npgsql;
using Quartz;
using Wireguard.Api.Data.Dtos;
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
                                 WITH peer_data AS (
                                     SELECT 
                                         DownloadVolume,
                                         UploadVolume
                                     FROM PEER 
                                     WHERE PublicKey = @PublicKey
                                 )
                                 UPDATE PEER
                                 SET 
                                     DownloadVolume = CASE 
                                                             WHEN @ReceivedBytes < peer_data.DownloadVolume 
                                                             THEN @ReceivedBytes + peer_data.DownloadVolume 
                                                             ELSE @ReceivedBytes 
                                                          END,
                                     UploadVolume = CASE 
                                                           WHEN @SentBytes < peer_data.UploadVolume 
                                                           THEN @SentBytes + peer_data.UploadVolume 
                                                           ELSE @SentBytes 
                                                        END,
                                     TotalReceivedVolume = CASE 
                                                                  WHEN (@ReceivedBytes + @SentBytes) < (peer_data.DownloadVolume + peer_data.UploadVolume)
                                                                  THEN (@ReceivedBytes + @SentBytes + peer_data.DownloadVolume + peer_data.UploadVolume)
                                                                  ELSE (@ReceivedBytes + @SentBytes)
                                                               END
                                 FROM peer_data
                                 WHERE PEER.PublicKey = @PublicKey;
                             """;
            try
            {
                Console.WriteLine($"{transferData.Count} peer transfers from {transferData.Count} files.");

                foreach (var transfer in transferData)
                {
                    
                    await connection.ExecuteAsync(command, new
                    {
                        ReceivedBytes = transfer.ReceivedBytes,
                        SentBytes = transfer.SentBytes,
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