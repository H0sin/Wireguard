using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Helpers;

public static class WireguardHelpers
{
    public static async Task<bool> CreatePeer(AddPeerDto peer, Interface @interface)
    {
        Console.WriteLine($"set {@interface.Name} peer {peer.PublicKey} allowed-ips {string.Join(",", peer.AllowedIPs)}/32 preshared-key {peer.PresharedKey}");
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "wg",
            Arguments = $"set {@interface.Name} peer {peer.PublicKey} allowed-ips ${string.Join(",", peer.AllowedIPs)} preshared-key {peer.PresharedKey}",
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
}