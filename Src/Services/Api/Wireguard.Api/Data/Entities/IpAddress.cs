using System.ComponentModel.DataAnnotations.Schema;
using Wireguard.Api.Data.Common;

namespace Wireguard.Api.Data.Entities;

public class IpAddress : EntityBase
{
    public long? InterfaceId { get; set; }
    public string? Ip { get; set; }
    public bool Available { get; set; } = false;
} 