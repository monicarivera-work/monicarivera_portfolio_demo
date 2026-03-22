using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class ArchitectureMapModel : PageModel
    {
        private readonly ILogger<ArchitectureMapModel> _logger;

        public ArchitectureMapModel(ILogger<ArchitectureMapModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Architecture Map page visited");
        }
    }
}
