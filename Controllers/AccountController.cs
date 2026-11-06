using System.Security.Claims;
using HRMS.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IConfiguration configuration, ILogger<AccountController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Configurable demo credentials (default: admin / 123)
        var configuredUser = _configuration["Auth:AdminUsername"] ?? "admin";
        var configuredPass = _configuration["Auth:AdminPassword"] ?? "123";

        var isUserValid = string.Equals(model.Username?.Trim(), configuredUser, StringComparison.OrdinalIgnoreCase);
        var isPassValid = string.Equals(model.Password, configuredPass, StringComparison.Ordinal);

        if (isUserValid && isPassValid)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, model.Username!.Trim()),
                new(ClaimTypes.Role, "Administrator"),
                new("DisplayName", "System Administrator"),
                new("LoginTime", DateTime.UtcNow.ToString("o"))
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
            _logger.LogInformation("User '{Username}' successfully logged into HRMS from {Ip}", model.Username, HttpContext.Connection.RemoteIpAddress);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        model.ErrorMessage = "Invalid username or password. Default demo credentials: admin / 123";
        _logger.LogWarning("Failed login attempt for username '{Username}'", model.Username);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User '{Username}' logged out", userName);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
