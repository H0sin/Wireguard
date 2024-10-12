using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine($"SyncPeer starting...");

        var transferData = await WireguardHelpers.GetTransferData();

        Console.WriteLine($"SyncPeer: {transferData}");

        if (transferData != null && transferData.Count > 0)
        {
            foreach (var transfer in transferData)
            {
                Console.WriteLine(transfer.PeerPublicKey);
                // logger.LogInformation(transfer.PeerPublicKey);
            }
        }

        await Task.CompletedTask;
    }
}