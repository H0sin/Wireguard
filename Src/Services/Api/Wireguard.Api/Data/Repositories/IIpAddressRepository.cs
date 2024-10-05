namespace Wireguard.Api.Data.Repositories;

public interface IIpAddressRepository
{
    Task<bool> ExistIpAddressAsync(string ipAddress);
    Task<bool> AddIpAddressAsync(string ipAddress,int interfaceId); 
}