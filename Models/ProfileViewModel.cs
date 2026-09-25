using System.ComponentModel.DataAnnotations;

namespace DevFolio.Models
{
    public class ProfileViewModel
    {
        public const int MaxProjects = 20;

        /// <summary>Null when adding a new profile; the profile id when editing.</summary>
        public int? Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        /// <summary>Current image when editing (display only; the controller never trusts a posted value).</summary>
        public string? ProfileImagePath { get; set; }

        [Display(Name = "Remove current image")]
        public bool RemoveImage { get; set; }

        public List<ProjectViewModel> Projects { get; set; } = [new()];

        public bool IsEdit => Id.HasValue;
    }

    public class ProjectViewModel
    {
        // Not [Required]: blank rows are dropped on save; a row with other data but no name is rejected in the controller.
        [Display(Name = "Project Name")]
        [StringLength(150)]
        public string? ProjectName { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "GitHub Link")]
        [Url]
        [StringLength(500)]
        public string? GithubLink { get; set; }

        public bool IsBlank =>
            string.IsNullOrWhiteSpace(ProjectName)
            && string.IsNullOrWhiteSpace(Description)
            && string.IsNullOrWhiteSpace(GithubLink);
    }
}
