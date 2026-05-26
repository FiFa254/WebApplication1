using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Profile
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(500)]
    public string? ProfileImagePath { get; set; }

    public List<ProfileProject> Projects { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
