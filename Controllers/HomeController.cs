using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private static readonly List<Permission> AvailablePermissions =
        [
            new Permission { Name = "Read", Description = "Allows viewing of resources" },
            new Permission { Name = "Write", Description = "Allows creating or modifying resources" },
            new Permission { Name = "Delete", Description = "Allows removal of resources" },
            new Permission { Name = "Execute", Description = "Allows executing actions or commands" },
            new Permission { Name = "Admin", Description = "Full access to system settings" }
        ];

        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var projects = await _context.ProfileProjects
                .Include(project => project.Profile)
                .OrderByDescending(project => project.Id)
                .ToListAsync();

            return View(projects);
        }

        // GET: /Home/AddProfile
        public IActionResult AddProfile()
        {
            return View(new ProfileViewModel());
        }

        // POST: /Home/AddProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var projects = model.Projects
                .Where(project =>
                    !string.IsNullOrWhiteSpace(project.ProjectName) ||
                    !string.IsNullOrWhiteSpace(project.Description) ||
                    !string.IsNullOrWhiteSpace(project.GithubLink))
                .ToList();

            model.ProfileImagePath = await SaveProfileImageAsync(model.ProfileImage);

            if (!ModelState.IsValid)
            {
                model.Projects = projects.Count > 0 ? projects : [new ProjectViewModel()];
                return View(model);
            }

            var profile = new Profile
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Bio = model.Bio,
                ProfileImagePath = model.ProfileImagePath,
                Projects = projects
                    .Select(project => new ProfileProject
                    {
                        ProjectName = project.ProjectName,
                        Description = project.Description,
                        GithubLink = project.GithubLink
                    })
                    .ToList()
            };

            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            model.Projects = projects;

            return View("AddProfileConfirmation", model);
        }

        private async Task<string?> SaveProfileImageAsync(IFormFile? image)
        {
            if (image is null || image.Length == 0)
            {
                return null;
            }

            const long maxImageSize = 2 * 1024 * 1024;
            if (image.Length > maxImageSize)
            {
                ModelState.AddModelError(nameof(ProfileViewModel.ProfileImage), "Profile image must not exceed 2 MB.");
                return null;
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".gif",
                ".webp"
            };

            var extension = Path.GetExtension(image.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(ProfileViewModel.ProfileImage), "Profile image must be JPG, PNG, GIF, or WebP.");
                return null;
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await image.CopyToAsync(stream);

            return $"/uploads/profiles/{fileName}";
        }

        // GET: /Home/Permission
        public async Task<IActionResult> Permission()
        {
            var selectedPermissions = await _context.PermissionGrants
                .Select(permission => permission.PermissionName)
                .ToArrayAsync();

            var model = new PermissionViewModel
            {
                Permissions = AvailablePermissions,
                SelectedPermissions = selectedPermissions
            };

            return View(model);
        }

        // POST: /Home/Permission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permission(PermissionViewModel model)
        {
            model.SelectedPermissions ??= [];

            var existingGrants = await _context.PermissionGrants.ToListAsync();
            _context.PermissionGrants.RemoveRange(existingGrants);

            var grants = model.SelectedPermissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(permissionName => new PermissionGrant { PermissionName = permissionName });

            _context.PermissionGrants.AddRange(grants);
            await _context.SaveChangesAsync();

            model.Permissions = AvailablePermissions;
            ViewData["PermissionSaved"] = true;
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
