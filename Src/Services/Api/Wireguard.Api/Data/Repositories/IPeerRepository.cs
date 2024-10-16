using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public interface IPeerRepository
{
    // Task ReasetPeer();
    Task<bool> InsertAsync(AddPeerDto peer, string interfaceName, CancellationToken cancellationToken = default);
    Task<FilterPeerDto> FilterPeerAsync(FilterPeerDto filter, CancellationToken cancellationToken = default);
    Task<string> GeneratePeerContentConfigAsync(string name);
    Task<Peer?> UpdatePeerAsync(UpdatePeerDto peer,string name);
    Task<Peer?> GetPeerByNameAsync(string name);
}