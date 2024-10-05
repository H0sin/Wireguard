using System.Net;
using Dapper;
using Npgsql;

namespace Wireguard.Api.Data.Repositories;

public class IpAddressRepository(IConfiguration configuration) : IIpAddressRepository
{
    public async Task<bool> ExistIpAddressAsync(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress), "IP Address cannot be null or empty");

        await using var connection =
            new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));

        string command = "SELECT COUNT(*) FROM IpAddress Where Ip = @ipAddress";

        int count = await connection.QueryFirstOrDefaultAsync<int>(command, new { ipAddress });

        return count > 0;
    }

    public async Task<bool> AddIpAddressAsync(string ipAddress, int interfaceId,NpgsqlConnection connection,NpgsqlTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentNullException(nameof(ipAddress), "IP Range cannot be null or empty");

        var (startIp, subnetMask) = ParseIpRange(ipAddress);
        var ipAddresses = GetIpRange(startIp, subnetMask);
        
        await connection.OpenAsync();
        
        try
        {
            foreach (var ip in ipAddresses)
            {
                string insertCommand = """
                                       INSERT INTO IpAddress (Ip, Available,InterfaceId) 
                                       VALUES (@Ip, false,@interfaceId)
                                       """;

                await connection.ExecuteAsync(insertCommand, new { Ip = ip, interfaceId },transaction);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new ApplicationException("An error occurred while inserting IP addresses", ex);
        }
    }

    private (string, int) ParseIpRange(string ipRange)
    {
        var parts = ipRange.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var subnet))
            throw new ArgumentException("Invalid IP range format. Use CIDR notation, e.g., '10.0.0.3/24'.");

        return (parts[0], subnet);
    }

    private IEnumerable<string> GetIpRange(string startIp, int subnetMask)
    {
        var ip = IPAddress.Parse(startIp);
        var ipBytes = ip.GetAddressBytes();


        for (int i = (ipBytes[3] + 1); i <= 255; ++i)
        {
            var newIpBytes = BitConverter.GetBytes(BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0) + (uint)i);
            yield return new IPAddress(newIpBytes.Reverse().ToArray()).ToString();
        }
    }
}