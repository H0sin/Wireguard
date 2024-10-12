using Dapper;
using Npgsql;
using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer(IServiceScopeFactory serviceScopeFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        Console.WriteLine($"SyncPeer starting...");

        var transferData = await WireguardHelpers.GetTransferData();

        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

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

            foreach (var transfer in transferData)
            {
                Console.WriteLine(transfer.PeerPublicKey);
                await connection.ExecuteAsync(command, new
                {
                    DownloadVolume = transfer.ReceivedBytes,
                    UploadVolume = transfer.SentBytes,
                    TotalReceivedVolume = transfer.ReceivedBytes + transfer.SentBytes
                }, transaction);
            }
        }

        await transaction.CommitAsync();
        await connection.CloseAsync();

        await Task.CompletedTask;
    }
}