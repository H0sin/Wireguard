using Quartz;
using Wireguard.Api.Helpers;

namespace Wireguard.Api.Jobs;

public class SyncPeer() : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // logger.LogInformation("job started");
        string wgs = await WireguardHelpers.WgShow();
        
        // logger.LogInformation("job endded");
        return;
    }

    private (double upload, double download) ParseWgShowOutput(string wgShowOutput)
    {
        Console.WriteLine("sssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss");
        Console.WriteLine(wgShowOutput);
        double upload = 0;
        double download = 0;
        
        // logger.LogInformation("0plolllllllllllllllllllllllllllllllllllllllllllllllll");
        //
        // logger.LogInformation(wgShowOutput);
        
        string[] lines = wgShowOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("transfer:"))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 4)
                {
                    upload = ConvertToMegabytes(parts[4]);
                    download = ConvertToMegabytes(parts[7]);
                }
            }
        }

        return (upload, download);
    }

    private double ConvertToMegabytes(string size)
    {
        double value = double.Parse(size.Substring(0, size.Length - 3)); // Remove the last 3 characters (e.g. " MiB")
        return size.EndsWith("MiB") ? value : value / 1024; // Convert to MB if not MiB
    }
}