using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using DevFolio.Infrastructure;

namespace DevFolio.Controllers;

public class AccountController : Controller
{
    private readonly AdminCredentials _admin;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IOptions<AdminCredentials> admin, ILogger<AccountController> logger)
    {
        _admin = admin.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["AdminConfigured"] = _admin.IsConfigured;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["AdminConfigured"] = _admin.IsConfigured;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!_admin.Verify(model.Username, model.Password))
        {
            _logger.LogWarning("Failed admin login for {Username} from {Ip}.", model.Username, HttpContext.Connection.RemoteIpAddress);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, _admin.Username!), new Claim(ClaimTypes.Role, "Admin")],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        _logger.LogInformation("Admin {Username} signed in.", _admin.Username);

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}

public class LoginViewModel
{
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(200)]
    public string Password { get; set; } = string.Empty;
}
