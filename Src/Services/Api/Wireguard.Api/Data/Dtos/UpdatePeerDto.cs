using Wireguard.Api.Data.Enums;

namespace Wireguard.Api.Data.Dtos;

public class UpdatePeerDto
{
    public string? EndPoint { get; set; }
    public string? Dns { get; set; }
    public int? Mtu { get; set; } = 1280;
    public int? PersistentKeepalive { get; set; } = 21;
    public string? EndpointAllowedIPs { get; set; } = "0.0.0.0/0";
    public long ExpireTime { get; set; } = 0;
    public long TotalVolume { get; set; } = 0;
}