namespace Wireguard.Api.Data.Dtos;

public class PeerDto
{
    public string InterfaceName { get; set; }
    public string? PublicKey { get; set; }
    public string Status { get; set; }
}