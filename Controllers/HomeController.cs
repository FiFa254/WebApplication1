using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using DevFolio.Data;
using DevFolio.Infrastructure;
using DevFolio.Models;

namespace DevFolio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ImageStorage _images;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ImageStorage images, ILogger<HomeController> logger)
        {
            _context = context;
            _images = images;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ProfileCount = await _context.Profiles.CountAsync();
            ViewBag.ProjectCount = await _context.ProfileProjects.CountAsync();
            ViewBag.RecentProfiles = await _context.Profiles
                .AsNoTracking()
                .OrderByDescending(profile => profile.CreatedAt)
                .Select(profile => new ProfileSummary(profile, profile.Projects.Count))
                .Take(3)
                .ToListAsync();
            ViewBag.RecentProjects = await _context.ProfileProjects
                .AsNoTracking()
                .Include(project => project.Profile)
                .OrderByDescending(project => project.Id)
                .Take(3)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> Profiles(int page = 1)
        {
            var query = _context.Profiles
                .AsNoTracking()
                .OrderByDescending(profile => profile.CreatedAt)
                .ThenByDescending(profile => profile.Id)
                .Select(profile => new ProfileSummary(profile, profile.Projects.Count));

            return View(await PagedList<ProfileSummary>.CreateAsync(query, page));
        }

        public async Task<IActionResult> Profile(int id)
        {
            var profile = await _context.Profiles
                .AsNoTracking()
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            return View(profile);
        }

        /// <summary>
        /// The e-mail address is never written into page HTML (scrapers harvest it); the Contact button
        /// hits this rate-limited endpoint, which redirects to the visitor's mail app.
        /// </summary>
        [EnableRateLimiting(RateLimitPolicies.Contact)]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Contact(int id)
        {
            var email = await _context.Profiles
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => p.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(email))
            {
                return NotFound();
            }

            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            return Redirect("mailto:" + Uri.EscapeDataString(email).Replace("%40", "@"));
        }

        public async Task<IActionResult> Projects(int page = 1)
        {
            var query = _context.ProfileProjects
                .AsNoTracking()
                .Include(project => project.Profile)
                .OrderByDescending(project => project.Id);

            return View(await PagedList<ProfileProject>.CreateAsync(query, page));
        }

        [Authorize]
        public IActionResult AddProfile()
        {
            return View("ProfileForm", new ProfileViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProfile(ProfileViewModel model)
        {
            model.Id = null;
            var projects = await ValidateAsync(model);
            if (!ModelState.IsValid)
            {
                return FormView(model, projects);
            }

            string? imageUrl = null;
            if (model.ProfileImage is { Length: > 0 })
            {
                (imageUrl, var error) = await _images.SaveProfileImageAsync(model.ProfileImage, HttpContext.RequestAborted);
                if (error is not null)
                {
                    ModelState.AddModelError(nameof(ProfileViewModel.ProfileImage), error);
                    return FormView(model, projects);
                }
            }

            var profile = new Profile
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                Bio = model.Bio?.Trim(),
                ProfileImagePath = imageUrl,
                Projects = projects.Select(ToEntity).ToList()
            };

            try
            {
                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();
            }
            catch
            {
                _images.Delete(imageUrl);
                throw;
            }

            _logger.LogInformation("Profile {ProfileId} created by {User}.", profile.Id, User.Identity?.Name);
            TempData["Status"] = "Profile saved.";
            return RedirectToAction(nameof(Profile), new { id = profile.Id });
        }

        [Authorize]
        public async Task<IActionResult> EditProfile(int id)
        {
            var profile = await _context.Profiles
                .AsNoTracking()
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Id = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Bio = profile.Bio,
                ProfileImagePath = profile.ProfileImagePath,
                Projects = profile.Projects
                    .OrderBy(project => project.Id)
                    .Select(project => new ProjectViewModel
                    {
                        ProjectName = project.ProjectName,
                        Description = project.Description,
                        GithubLink = project.GithubLink
                    })
                    .ToList()
            };

            if (model.Projects.Count == 0)
            {
                model.Projects.Add(new ProjectViewModel());
            }

            return View("ProfileForm", model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(int id, ProfileViewModel model)
        {
            var profile = await _context.Profiles
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profile is null)
            {
                return NotFound();
            }

            model.Id = id;
            model.ProfileImagePath = profile.ProfileImagePath;

            var projects = await ValidateAsync(model);
            if (!ModelState.IsValid)
            {
                return FormView(model, projects);
            }

            var oldImage = profile.ProfileImagePath;
            string? newImage = null;
            if (model.ProfileImage is { Length: > 0 })
            {
                (newImage, var error) = await _images.SaveProfileImageAsync(model.ProfileImage, HttpContext.RequestAborted);
                if (error is not null)
                {
                    ModelState.AddModelError(nameof(ProfileViewModel.ProfileImage), error);
                    return FormView(model, projects);
                }

                profile.ProfileImagePath = newImage;
            }
            else if (model.RemoveImage)
            {
                profile.ProfileImagePath = null;
            }

            profile.FirstName = model.FirstName.Trim();
            profile.LastName = model.LastName.Trim();
            profile.Email = model.Email.Trim();
            profile.Bio = model.Bio?.Trim();

            _context.ProfileProjects.RemoveRange(profile.Projects);
            profile.Projects = projects.Select(ToEntity).ToList();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                _images.Delete(newImage);
                throw;
            }

            if (oldImage != profile.ProfileImagePath)
            {
                _images.Delete(oldImage);
            }

            _logger.LogInformation("Profile {ProfileId} updated by {User}.", profile.Id, User.Identity?.Name);
            TempData["Status"] = "Profile updated.";
            return RedirectToAction(nameof(Profile), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Projects)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (profile is null)
            {
                return NotFound();
            }

            // Projects are loaded so EF removes them on every provider (the database FK also cascades).
            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();
            _images.Delete(profile.ProfileImagePath);

            _logger.LogInformation("Profile {ProfileId} deleted by {User}.", id, User.Identity?.Name);
            TempData["Status"] = $"Profile of {profile.FirstName} {profile.LastName} deleted.";
            return RedirectToAction(nameof(Profiles));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Drops blank project rows, then checks what attributes cannot: project count, a name on every
        /// remaining row, and a unique e-mail. Returns the projects to save.
        /// </summary>
        private async Task<List<ProjectViewModel>> ValidateAsync(ProfileViewModel model)
        {
            var rows = model.Projects ?? [];
            var projects = rows.Where(project => !project.IsBlank).ToList();

            if (projects.Count > ProfileViewModel.MaxProjects)
            {
                ModelState.AddModelError(string.Empty, $"A profile can have at most {ProfileViewModel.MaxProjects} projects.");
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].IsBlank && string.IsNullOrWhiteSpace(rows[i].ProjectName))
                {
                    ModelState.AddModelError($"Projects[{i}].ProjectName", "The Project Name field is required.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var email = model.Email.Trim().ToLower();
                var taken = await _context.Profiles.AnyAsync(p => p.Email.ToLower() == email && p.Id != (model.Id ?? 0));
                if (taken)
                {
                    ModelState.AddModelError(nameof(ProfileViewModel.Email), "Another profile already uses this e-mail.");
                }
            }

            return projects;
        }

        private ViewResult FormView(ProfileViewModel model, List<ProjectViewModel> projects)
        {
            // Keep rows the user typed (validation messages point at their indexes) unless all were blank.
            if (model.Projects is null || model.Projects.Count == 0 || projects.Count == 0)
            {
                model.Projects = [new ProjectViewModel()];
            }

            return View("ProfileForm", model);
        }

        private static ProfileProject ToEntity(ProjectViewModel project) => new()
        {
            ProjectName = project.ProjectName!.Trim(),
            Description = project.Description?.Trim(),
            GithubLink = project.GithubLink?.Trim()
        };
    }
}
