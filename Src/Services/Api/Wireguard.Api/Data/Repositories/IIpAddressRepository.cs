using System.Net;
using Npgsql;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public interface IIpAddressRepository
{
    Task<bool> ExistIpAddressAsync(string ipAddress);

    Task<bool> AddIpAddressAsync(string ipAddress, int interfaceId, NpgsqlConnection connection,
        NpgsqlTransaction transaction);

    Task<List<IpAddress?>> GetIpAddressByInterfaceIdAsync(int interfaceId);
    Task<bool> OutOfReachIpAddressAsync(List<int> ids,NpgsqlConnection connection,
        NpgsqlTransaction transaction);
}