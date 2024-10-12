using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Helpers;

public static class WireguardHelpers
{
    public static async Task<List<WireGuardTransfer>> GetTransferDataAsync()
    {
        var transferData = new List<WireGuardTransfer>();

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "wg",
                Arguments = "show all transfer",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    outputBuilder.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    errorBuilder.AppendLine(args.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorOutput = errorBuilder.ToString();
                throw new Exception($"فرآیند wg با کد خروجی {process.ExitCode} خاتمه یافت: {errorOutput}");
            }

            var output = outputBuilder.ToString();
            // ثبت خروجی برای دیباگ
            Console.WriteLine("خروجی wg:\n" + output);

            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            // تعریف الگوی Regex برای تطبیق خطوط با چهار بخش
            var regex = new Regex(@"^(?<iface>\S+)\s+(?<peer>\S+)\s+(?<received>\d+)\s+(?<sent>\d+)$");

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                Console.WriteLine($"در حال پردازش خط: {trimmedLine}");

                var match = regex.Match(trimmedLine);
                if (!match.Success)
                {
                    Console.WriteLine($"خط نادیده گرفته شد به دلیل عدم تطابق با الگوی مورد انتظار: {trimmedLine}");
                    continue;
                }

                string iface = match.Groups["iface"].Value;
                string peer = match.Groups["peer"].Value;
                string received = match.Groups["received"].Value;
                string sent = match.Groups["sent"].Value;

                bool receivedParsed = long.TryParse(received, out long receivedBytes);
                bool sentParsed = long.TryParse(sent, out long sentBytes);

                if (!receivedParsed)
                {
                    receivedBytes = 0;
                    Console.WriteLine($"عدم موفقیت در پارس کردن تعداد بایت‌های دریافتی برای خط: {trimmedLine}");
                }

                if (!sentParsed)
                {
                    sentBytes = 0;
                    Console.WriteLine($"عدم موفقیت در پارس کردن تعداد بایت‌های ارسال شده برای خط: {trimmedLine}");
                }

                // **تغییر: حذف فیلتر مقادیر صفر**
                transferData.Add(new WireGuardTransfer
                {
                    Interface = iface,
                    PeerPublicKey = peer,
                    ReceivedBytes = receivedBytes,
                    SentBytes = sentBytes
                });

                Console.WriteLine($"اضافه شد: {iface}: {peer}: {receivedBytes}/{sentBytes}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"خطا در GetTransferDataAsync: {ex.Message}");
            // بازگشت یک لیست خالی به جای null برای جلوگیری از خطاهای مرجع خالی
            return new List<WireGuardTransfer>();
        }

        // ثبت تعداد داده‌های افزوده شده برای دیباگ
        Console.WriteLine($"تعداد داده‌های افزوده شده: {transferData.Count}");

        // ثبت اطلاعات اولین عنصر در لیست اگر موجود باشد
        if (transferData.Count > 0)
        {
            Console.WriteLine($"اولین اینترفیس: {transferData[0].Interface}");
        }
        else
        {
            Console.WriteLine("هیچ داده انتقالی موجود نیست.");
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
            Arguments =
                $"set {@interface.Name} peer {peer.PublicKey} allowed-ips {string.Join(",", peer.AllowedIPs)}",
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