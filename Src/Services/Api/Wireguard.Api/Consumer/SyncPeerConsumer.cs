using Dapper;
using EventBus.Messages.Events;
using MassTransit;
using Npgsql;
using Wireguard.Api.Helpers;
using System.Data;
using Wireguard.Api.Data.Dtos;

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
            // Create a temporary table to hold the data
            string createTempTableQuery = """
                CREATE TEMP TABLE TempPeerData (
                    PublicKey VARCHAR PRIMARY KEY,
                    ReceivedBytes BIGINT,
                    SentBytes BIGINT
                );
                """;

            await connection.ExecuteAsync(createTempTableQuery);

            // Insert data into the temporary table
            using (var importer = connection.BeginBinaryImport("COPY TempPeerData (PublicKey, ReceivedBytes, SentBytes) FROM STDIN (FORMAT BINARY)"))
            {
                foreach (var transfer in transferData)
                {
                    importer.StartRow();
                    importer.Write(transfer.PeerPublicKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                    importer.Write(transfer.ReceivedBytes, NpgsqlTypes.NpgsqlDbType.Bigint);
                    importer.Write(transfer.SentBytes, NpgsqlTypes.NpgsqlDbType.Bigint);
                }

                await importer.CompleteAsync();
            }

            // Update the PEER table using the data from the temporary table
            string updateQuery = """
                WITH peer_data AS (
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
                        t.PublicKey,
                        p.Status,
                        p.ExpireTime,
                        p.OnHoldExpireDurection,
                        i.Name AS InterfaceName
                    FROM PEER p
                    JOIN Interface i ON p.InterfaceId = i.Id
                    JOIN TempPeerData t ON p.PublicKey = t.PublicKey
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
                    END,
                    Status = CASE
                        WHEN peer_data.Status = 'onhold' AND peer_data.TotalReceivedVolume != 0 THEN 'active'
                        WHEN peer_data.TotalReceivedVolume - peer_data.TotalVolume > 0 AND peer_data.Status = 'active' THEN 'limited'
                        WHEN peer_data.ExpireTime < EXTRACT(EPOCH FROM NOW()) AND peer_data.Status != 'onhold' THEN 'expired'
                        ELSE peer_data.Status
                    END,
                    ExpireTime = CASE
                        WHEN peer_data.Status = 'onhold' AND peer_data.TotalReceivedVolume != 0 THEN EXTRACT(EPOCH FROM NOW()) * 1000 + peer_data.OnHoldExpireDurection
                        ELSE peer_data.ExpireTime
                    END
                FROM peer_data
                WHERE PEER.PublicKey = peer_data.PublicKey
                RETURNING peer_data.InterfaceName, PEER.PublicKey, PEER.Status;
                """;

            IEnumerable<PeerDto> updatedPeers;
            try
            {
                updatedPeers = await connection.QueryAsync<PeerDto>(updateQuery);
            }
            catch (Exception e)
            {
                Console.WriteLine("Throw exception: " + e.Message);
                return;
            }

            // Remove expired or limited peers based on updated status
            foreach (var peer in updatedPeers)
            {
                if (peer.Status == "expired" || peer.Status == "limited" || peer.Status == "disabled")
                {
                    await WireguardHelpers.RemovePeer(peer.InterfaceName, peer.PublicKey);
                    await WireguardHelpers.Save(peer.InterfaceName);
                }
            }
        }
        else
        {
            Console.WriteLine("No data found");
        }

        await connection.CloseAsync();
        await Task.CompletedTask;
    }
}
