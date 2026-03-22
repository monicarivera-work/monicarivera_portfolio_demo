using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages
{
    public class AskMonicaModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string? ContactEmail { get; private set; }

        public AskMonicaModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            ContactEmail = _configuration[Constants.EmailAddressKey];
        }
    }
}
