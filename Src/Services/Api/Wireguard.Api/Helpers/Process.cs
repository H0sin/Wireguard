using Microsoft.AspNetCore.Http.HttpResults;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Helpers;

public static class Process
{
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
}