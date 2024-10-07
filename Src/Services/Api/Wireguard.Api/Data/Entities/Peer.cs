using Wireguard.Api.Data.Common;

namespace Wireguard.Api.Data.Entities;

public class Peer : EntityBase
{
    public int InterfaceId { get; set; }

    public string? Name { get; set; }

    public string? PublicKey { get; set; }

    public string? PrivateKey { get; set; }

    public string? PresharedKey { get; set; }

    public string? AllowedIPs { get; set; }

    public string? EndPoint { get; set; }

    public string? EndpointAllowedIPs { get; set; }

    public string? Dns { get; set; }

    public int? Mtu { get; set; } = 1420;

    public int? PersistentKeepalive { get; set; } = 21;
}