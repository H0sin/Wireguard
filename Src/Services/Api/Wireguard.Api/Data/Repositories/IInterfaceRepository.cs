using System.Linq.Expressions;
using Wireguard.Api.Data.Common;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;
using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Data.Repositories;

public interface IInterfaceRepository
{
    Task<Interface?> GetInterfaceByNameAsync(string name);
    Task<ICollection<Interface>> GetAllAsync();
    Task<bool> InsertAsync(AddInterfaceDto entity);
    Task<bool> ChangeStatusInterfaceAsync(string name,InterfaceStatus status);
}