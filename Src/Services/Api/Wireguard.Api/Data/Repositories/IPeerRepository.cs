using Wireguard.Api.Data.Dtos;
using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Repositories;

public interface IPeerRepository
{
    // Task ReasetPeer();
    Task<bool> InsertAsync(AddPeerDto peer, string interfaceName, CancellationToken cancellationToken = default);
    Task<FilterPeerDto> FilterPeerAsync(FilterPeerDto filter, CancellationToken cancellationToken = default);
    Task<string> GeneratePeerContentConfigAsync(string name);
    Task<Peer?> UpdatePeerAsync(UpdatePeerDto peer, string name);
    Task<Peer?> GetPeerByNameAsync(string name);
    Task<Peer?> ResetPeerAsync(ResetPeerDto peer, string name);
    Task<Peer?> DisabledPeerAsync(string name);
    Task<Peer?> ActivePeerAsync(string name);
    Task DeletePeerAsync(string name);
}