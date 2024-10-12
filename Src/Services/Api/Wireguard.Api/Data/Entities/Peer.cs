using Wireguard.Api.Data.Common;
using Wireguard.Api.Data.Enums;

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

    public string? EndpointAllowedIPs { get; set; } = "0.0.0.0/0";
    public string? Dns { get; set; }
    public int? Mtu { get; set; } = 1420;
    public int? PersistentKeepalive { get; set; } = 21;

    public long TotalReceivedVolume { get; set; }

    public long DownloadVolume { get; set; }

    public long UploadVolume { get; set; }

    public long StartTime { get; set; }

    public long ExpireTime { get; set; }

    public long TotalVolume  { get; set; }

    public string Status { get; set; } = PeerStatus.OnHold.ToString();

    public long OnHoldExpireDurection { get; set; }
}