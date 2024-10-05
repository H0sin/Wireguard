using Wireguard.Api.Data.Common;

namespace Wireguard.Api.Data.Entities;

public class Peer : EntityBase
{
    public string InterfaceId { get; set; }

    public string? Name { get; set; }

    public string? PublicKey { get; set; }

    public string? PresharedKey { get; set; }

    public List<string>? AllowedIPs { get; set; }

    public string? EndPoint { get; set; }
}