using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        public string? ProfileImagePath { get; set; }

        public List<ProjectViewModel> Projects { get; set; } = [new()];
    }

    public class ProjectViewModel
    {
        [Required]
        [Display(Name = "Project Name")]
        [StringLength(150)]
        public string ProjectName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "GitHub Link")]
        [Url]
        [StringLength(500)]
        public string? GithubLink { get; set; }

    }
}
