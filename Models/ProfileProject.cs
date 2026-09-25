using System.ComponentModel.DataAnnotations;

namespace DevFolio.Models;

public class ProfileProject
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    public Profile? Profile { get; set; }

    [Required]
    [StringLength(150)]
    public string ProjectName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Url]
    [StringLength(500)]
    public string? GithubLink { get; set; }

}
