namespace Wireguard.Api.Data.Dtos;

public class InterfaceDetailDto
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Port { get; set; }
    public string PublicKey { get; set; }
    public long TotalData { get; set; }
    public long TotoalDataUsed { get; set; }
    public string Status { get; set; }
}