namespace Wireguard.Api.Data.Dtos;

public class PeerVolumeDto
{
    public long DownloadVolume { get; set; }
    public long UploadVolume { get; set; }
}