using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages;

public class MiniGameModel : PageModel
{
    private readonly ILogger<MiniGameModel> _logger;

    public MiniGameModel(ILogger<MiniGameModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("Mini Game page visited");
    }
}
