using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var transferData = WireguardHelpers.GetTransferData();

                if (transferData != null && transferData.Count > 0)
                {
                    Console.WriteLine(transferData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "An error occurred while processing transfer data.");
            }
        }
    }
}