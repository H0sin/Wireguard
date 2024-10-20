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
            try
            {
                var transferDataJson = System.Text.Json.JsonSerializer.Serialize(transferData.Select(t => new
                {
                    t.PeerPublicKey,
                    t.ReceivedBytes,
                    t.SentBytes
                }));

                string command = """
                                 WITH transfer_data AS (
                                     SELECT * FROM jsonb_to_recordset(@TransferData::jsonb)
                                     AS t(PeerPublicKey text, ReceivedBytes bigint, SentBytes bigint)
                                 ),
                                 peer_data AS (
                                     SELECT 
                                         p.LastDownloadVolume,
                                         p.LastUploadVolume,
                                         p.LastTotalReceivedVolume,
                                         p.DownloadVolume,
                                         p.UploadVolume,
                                         p.TotalReceivedVolume,
                                         i.UploadPercent,
                                         i.DownloadPercent,
                                         t.ReceivedBytes,
                                         t.SentBytes,
                                         t.PeerPublicKey
                                     FROM PEER p
                                     JOIN Interface i ON p.InterfaceId = i.Id
                                     JOIN transfer_data t ON p.PublicKey = t.PeerPublicKey
                                 )
                                 UPDATE PEER
                                 SET
                                     LastDownloadVolume = peer_data.ReceivedBytes,
                                     LastUploadVolume = peer_data.SentBytes,
                                     LastTotalReceivedVolume = peer_data.SentBytes + peer_data.ReceivedBytes,                          
                                     UploadVolume = CASE 
                                                        WHEN peer_data.ReceivedBytes >= peer_data.LastDownloadVolume 
                                                        THEN ROUND((peer_data.ReceivedBytes - peer_data.LastDownloadVolume) * peer_data.DownloadPercent) + peer_data.DownloadVolume 
                                                        ELSE ROUND(peer_data.ReceivedBytes * peer_data.DownloadPercent) + peer_data.DownloadVolume
                                                    END,
                                     DownloadVolume = CASE 
                                                         WHEN peer_data.SentBytes >= peer_data.LastUploadVolume 
                                                         THEN ROUND((peer_data.SentBytes - peer_data.LastUploadVolume) * peer_data.UploadPercent) + peer_data.UploadVolume 
                                                         ELSE ROUND(peer_data.SentBytes * peer_data.UploadPercent) + peer_data.UploadVolume
                                                     END,
                                     TotalReceivedVolume = CASE 
                                                              WHEN (peer_data.ReceivedBytes + peer_data.SentBytes) >= peer_data.LastTotalReceivedVolume 
                                                              THEN ROUND(((peer_data.ReceivedBytes + peer_data.SentBytes) - peer_data.LastTotalReceivedVolume) * peer_data.UploadPercent * peer_data.DownloadPercent) + peer_data.TotalReceivedVolume 
                                                              ELSE ROUND((peer_data.ReceivedBytes + peer_data.SentBytes) * peer_data.UploadPercent * peer_data.DownloadPercent) + peer_data.TotalReceivedVolume
                                                           END
                                 FROM peer_data
                                 WHERE PEER.PublicKey = peer_data.PeerPublicKey;
                                 """;

                await connection.ExecuteAsync(command, new { TransferData = transferDataJson });
            }
            catch (Exception e)
            {
                await connection.CloseAsync();
                Console.WriteLine("Exception occurred: " + e.Message);
            }
        }
        else
        {
            Console.WriteLine("no data found");
            await connection.CloseAsync();
        }

        await Task.CompletedTask;
    }
}