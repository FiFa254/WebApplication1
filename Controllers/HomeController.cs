using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var profileCount = await _context.Profiles.CountAsync();
            var projectCount = await _context.ProfileProjects.CountAsync();
            var recentProfiles = await _context.Profiles
                .OrderByDescending(profile => profile.CreatedAt)
                .Take(3)
                .ToListAsync();
            var recentProjects = await _context.ProfileProjects
                .Include(project => project.Profile)
                .OrderByDescending(project => project.Id)
                .Take(3)
                .ToListAsync();

            ViewBag.ProfileCount = profileCount;
            ViewBag.ProjectCount = projectCount;
            ViewBag.RecentProfiles = recentProfiles;
            ViewBag.RecentProjects = recentProjects;

            return View();
        }

        public async Task<IActionResult> Profiles()
        {
            var profiles = await _context.Profiles
                .Include(profile => profile.Projects)
                .OrderByDescending(profile => profile.CreatedAt)
                .ToListAsync();

            return View(profiles);
        }

        public async Task<IActionResult> Profile(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            return View(profile);
        }

        public async Task<IActionResult> Projects()
        {
            var projects = await _context.ProfileProjects
                .Include(project => project.Profile)
                .OrderByDescending(project => project.Id)
                .ToListAsync();

            return View(projects);
        }

        public IActionResult Contact()
        {
            return RedirectToAction(nameof(Projects));
        }

        public IActionResult AddProfile()
        {
            return View(new ProfileViewModel());
        }

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
