using System.Linq.Expressions;
using Wireguard.Api.Data.Common;
using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public interface IInterfaceRepository
{
    Task<ICollection<Interface>> GetAllAsync();
    
    Task<bool> InsertAsync(AddInterfaceDto entity);
}