namespace Wireguard.Api.Data.Dtos;

public class AddPeerDto
{
    public string? Name { get; set; }
    public string? PublicKey { get; set; }
    public string? PresharedKey { get; set; }
    public List<string>? AllowedIPs { get; set; }

    public string? EndPoint { get; set; }
}