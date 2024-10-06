using Wireguard.Api.Data.Entities;

namespace Wireguard.Api.Data.Dtos;

public class FilterPeerDto
{
    public int Take { get; set; }
    public int Skip { get; set; }
    public string? Name { get; set; }
    public string InterfaceName { get; set; }
    public string PublicKey { get; set; }
    public int CountPeer { get; set; }
    public List<Peer> Peers { get; set; }
}