using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PortfolioDemo.Pages.Bunion.Admin;

public class LoginModel : PageModel
{
    private readonly IConfiguration _config;
    public string ErrorMessage { get; private set; } = string.Empty;

    public LoginModel(IConfiguration config)
    {
        _config = config;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.HasClaim("bunion_admin", "true"))
            return RedirectToPage("/Bunion/Admin/Dashboard");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string password)
    {
        var adminPassword = _config["Bunion:AdminPassword"] ?? "bunion-admin";

        if (string.IsNullOrWhiteSpace(password) || !ConstantTimeEquals(password, adminPassword))
        {
            ErrorMessage = "Invalid password. Please try again.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "bunion_admin"),
            new("bunion_admin", "true")
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc   = DateTimeOffset.UtcNow.AddDays(7)
            });

        return RedirectToPage("/Bunion/Admin/Dashboard");
    }

    /// <summary>Timing-safe string comparison to prevent timing attacks on the password.</summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
