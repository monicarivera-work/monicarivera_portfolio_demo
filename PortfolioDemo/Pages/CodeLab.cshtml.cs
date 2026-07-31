using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class CodeLabModel : PageModel
    {
        private readonly ILogger<CodeLabModel> _logger;

        public bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;
        public string? UserDisplayName => User.FindFirst("name")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        public CodeLabModel(ILogger<CodeLabModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("CodeLab page visited. Authenticated: {Auth}", IsAuthenticated);
        }
    }
}
