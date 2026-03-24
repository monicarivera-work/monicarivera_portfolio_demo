using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class SkillsModel : PageModel
    {
        private readonly ILogger<SkillsModel> _logger;
        private readonly IConfiguration _configuration;

        public string ResumeFileName { get; private set; } = string.Empty;

        public SkillsModel(ILogger<SkillsModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            _logger.LogInformation("Skills page visited");
            ResumeFileName = _configuration[Constants.ResumeFileNameKey] ?? string.Empty;
        }
    }

}
