using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class PermissionGrant
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string PermissionName { get; set; } = string.Empty;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
