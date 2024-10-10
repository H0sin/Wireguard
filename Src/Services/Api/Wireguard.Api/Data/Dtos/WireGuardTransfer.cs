namespace Wireguard.Api.Data.Dtos;

public class WireGuardTransfer
{
    public string Interface { get; set; }
    public string PeerPublicKey { get; set; }
    public long ReceivedBytes { get; set; }
    public long SentBytes { get; set; }
}