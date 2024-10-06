using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public interface IPeerRepository
{
    Task<bool> InsertAsync(AddPeerDto peer, string interfaceName, CancellationToken cancellationToken = default);
    Task<FilterPeerDto> FilterPeerAsync(FilterPeerDto filter, CancellationToken cancellationToken = default);
}