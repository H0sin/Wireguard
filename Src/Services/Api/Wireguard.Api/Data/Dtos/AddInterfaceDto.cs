namespace Wireguard.Api.Data.Dtos;

public class AddInterfaceDto
{
    public string? Address { get; set; }
    public string? EndPoint { get; set; }
    public bool SaveConfig { get; set; }
    public string? PreUp { get; set; } = "";
    public string? PostUp { get; set; } = "";
    public string? PreDown { get; set; } = "";
    public string? PostDown { get; set; } = "";
    public string? ListenPort { get; set; } = "";
    public string? PrivateKey { get; set; }
    public string? IpAddress { get; set; }
    public string? PublicKey { get; set; }
}