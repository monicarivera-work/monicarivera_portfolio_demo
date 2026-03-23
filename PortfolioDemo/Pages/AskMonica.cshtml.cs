using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class AskMonicaModel : PageModel
    {
        private readonly ILogger<AskMonicaModel> _logger;
        private readonly IConfiguration _configuration;

        public string? ContactEmail { get; private set; }

        public AskMonicaModel(ILogger<AskMonicaModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            _logger.LogInformation("Ask Monica AI page visited");
            ContactEmail = _configuration[Constants.EmailAddressKey];
        }
    }
}
