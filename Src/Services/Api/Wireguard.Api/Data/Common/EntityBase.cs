using System.ComponentModel.DataAnnotations;

namespace Wireguard.Api.Data.Common;

public class EntityBase
{
    public DateTime? CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdateDate { get; set; } = DateTime.UtcNow;

    [Key] public int Id { get; set; }
}