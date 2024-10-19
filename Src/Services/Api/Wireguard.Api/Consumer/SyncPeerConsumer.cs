using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Consumer;

public class SyncPeerConsumer(IConfiguration configuration) : IConsumer<SyncPeerEvent>
{
    public async Task Consume(ConsumeContext<SyncPeerEvent> context)
    {
        var transferData = await WireguardHelpers.GetTransferDataAsync();

        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        await connection.OpenAsync();

        if (transferData != null && transferData.Count > 0)
        {
            string command = """
                             WITH peer_data AS (
                                 SELECT 
                                     p.LastDownloadVolume,
                                     p.LastUploadVolume,
                                     p.LastTotalReceivedVolume,
                                     p.DownloadVolume,
                                     p.UploadVolume,
                                     p.TotalReceivedVolume,
                                     i.UploadPercent,
                                     i.DownloadPercent
                                 FROM PEER p
                                 JOIN Interface i ON p.InterfaceId = i.Id
                                 WHERE p.PublicKey = @PublicKey
                             )
                             UPDATE PEER
                             SET
                                 LastDownloadVolume = @ReceivedBytes,
                                 LastUploadVolume = @SentBytes,
                                 LastTotalReceivedVolume = @SentBytes + @ReceivedBytes,                          
                                 UploadVolume = CASE 
                                                    WHEN @ReceivedBytes >= peer_data.LastDownloadVolume 
                                                    THEN ROUND((@ReceivedBytes - peer_data.LastDownloadVolume) * peer_data.DownloadPercent) + peer_data.DownloadVolume 
                                                    ELSE ROUND(@ReceivedBytes * peer_data.DownloadPercent) + peer_data.DownloadVolume
                                                END,
                                 DownloadVolume = CASE 
                                                     WHEN @SentBytes >= peer_data.LastUploadVolume 
                                                     THEN ROUND((@SentBytes - peer_data.LastUploadVolume) * peer_data.UploadPercent) + peer_data.UploadVolume 
                                                     ELSE ROUND(@SentBytes * peer_data.UploadPercent) + peer_data.UploadVolume
                                                 END,
                                 TotalReceivedVolume = CASE 
                                                          WHEN (@ReceivedBytes + @SentBytes) >= peer_data.LastTotalReceivedVolume 
                                                          THEN ROUND(((@ReceivedBytes + @SentBytes) - peer_data.LastTotalReceivedVolume) * peer_data.UploadPercent * peer_data.DownloadPercent) + peer_data.TotalReceivedVolume 
                                                          ELSE ROUND((@ReceivedBytes + @SentBytes) * peer_data.UploadPercent * peer_data.DownloadPercent) + peer_data.TotalReceivedVolume
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
                        PublicKey = transfer.PeerPublicKey,
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("thorw exception :" + e.Message);
            }
        }
        else
        {
            Console.WriteLine("no data found");
        }

        await Task.CompletedTask;
    }
}