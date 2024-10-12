using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Helpers;

public static class WireguardHelpers
{
    public static async Task<List<WireGuardTransfer>> GetTransferData()
    {
        var transferData = new List<WireGuardTransfer>();

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "wg",
                Arguments = "show all transfer",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);

            if (process == null)
                throw new Exception("Failed to start wg process.");

            string output = await process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();

            Console.WriteLine(output);

            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    string iface = parts[0];
                    string peer = parts[1];
                    string received = parts[2];
                    string sent = parts[3];

                    long receivedBytes = ParseDataSize(received);
                    long sentBytes = ParseDataSize(sent);

                    transferData.Add(new WireGuardTransfer
                    {
                        Interface = iface,
                        PeerPublicKey = peer,
                        ReceivedBytes = receivedBytes,
                        SentBytes = sentBytes
                    });

                    Console.WriteLine(iface + ": " + peer + ": " + receivedBytes + "/" + sentBytes);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }

        return transferData;
    }

    private static long ParseDataSize(string data)
    {
        if (data == "0")
            return 0;

        if (long.TryParse(data, out long bytes))
        {
            return bytes;
        }

        return 0;
    }

    public static async Task<bool> CreatePeer(AddPeerDto peer, Interface @interface)
    {
        Console.WriteLine(
            $"set {@interface.Name} peer {peer.PublicKey} allowed-ips {string.Join(",", peer.AllowedIPs)}/32");
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "wg",
            Arguments = $"set {@interface.Name} peer {peer.PublicKey} allowed-ips {string.Join(",", peer.AllowedIPs)}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            UserName = "root"
        };

        try
        {
            using Process process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine("Output: " + output);
            }
            else
            {
                Console.WriteLine("No output received from wg show command.");
            }

            await Save(@interface.Name);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }

    public static async Task<bool> AddInterfaceFile(AddInterfaceDto @interface, string? directory = "")
    {
        try
        {
            string content = $"""
                              [Interface]
                              Address = {@interface.IpAddress}
                              SaveConfig = {@interface.SaveConfig}
                              PreUp = {@interface.PreUp ?? ""}
                              PostUp = {@interface.PostUp ?? ""}
                              PreDown = {@interface.PreDown ?? ""}
                              PostDown = {@interface.PostDown ?? ""} 
                              ListenPort = {@interface.ListenPort ?? ""}
                              PrivateKey = {@interface.PrivateKey}
                              """;

            string filePath = Path.Combine(directory, $"{@interface.Name}.conf");

            if (Directory.Exists(filePath))
                throw new ApplicationException($"file by name:{@interface.Name} already exists");

            await using StreamWriter writer = new StreamWriter(filePath, append: false);

            await writer.WriteLineAsync(content);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static async Task<(string, bool)> StatusWireguard(InterfaceStatus status, string? name = "")
    {
        string up = "up ";
        string down = "down ";

        string? arguments = (status == InterfaceStatus.active ? up : down) + name;

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "wg-quick",
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            UserName = "root"
        };

        try
        {
            using Process process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine("Output: " + output);
            }
            else
            {
                Console.WriteLine("No output received from wg show command.");
            }

            return (output, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e.Message);
            return (e.Message, false);
        }
    }

    public static async Task<bool> DeleteInterfaceFile(string path)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "rm",
            Arguments = $"-r {path}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            UserName = "root"
        };

        using Process process = Process.Start(psi);
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine("Output: " + output);
        }
        else
        {
            Console.WriteLine("No output received from wg show command.");
        }

        return true;
    }

    public static async Task<bool> Save(string name)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "wg-quick",
            Arguments = $"save {name}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            UserName = "root"
        };
        using Process process = Process.Start(psi);
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine("Output: " + output);
        }
        else
        {
            Console.WriteLine("No output received from wg show command.");
        }

        return true;
    }

    public static async Task<string> WgShow()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "wg",
            Arguments = "show",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            UserName = "root"
        };

        using Process process = Process.Start(psi);
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine("Output: " + output);
        }
        else
        {
            Console.WriteLine("No output received from wg show command.");
        }

        return output;
    }
}