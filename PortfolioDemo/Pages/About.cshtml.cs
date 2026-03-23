using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class AboutModel : PageModel
    {
        private readonly ILogger<AboutModel> _logger;
        private readonly IConfiguration _configuration;

        public string? ContactEmail { get; private set; }

        public AboutModel(ILogger<AboutModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            _logger.LogInformation("About page visited");
            ContactEmail = _configuration[Constants.EmailAddressKey];
        }
    }

}
