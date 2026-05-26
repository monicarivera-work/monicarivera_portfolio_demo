using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class LeetCodeModel : PageModel
    {
        private readonly ILogger<LeetCodeModel> _logger;

        public LeetCodeModel(ILogger<LeetCodeModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("LeetCode analytics page visited");
        }
    }
}
