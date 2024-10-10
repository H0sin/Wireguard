using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer(ILogger<SyncPeer> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("SyncPeer starting...");

        var transferData = await WireguardHelpers.GetTransferData();

        logger.LogInformation(transferData.ToString());
        
        if (transferData != null && transferData.Count > 0)
        {
            foreach (var transfer in transferData)
            {
                logger.LogInformation(transfer.PeerPublicKey);
            }
        }
        
        await Task.CompletedTask;
    }
}
