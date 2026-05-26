using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class SignInModel : PageModel
    {
        public IActionResult OnGet(string? returnUrl = "/CodeLab")
        {
            return Challenge(new AuthenticationProperties
            {
                RedirectUri = returnUrl ?? "/CodeLab"
            }, OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}
