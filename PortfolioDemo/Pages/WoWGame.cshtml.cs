using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages;

public class WoWGameModel : PageModel
{
    private readonly ILogger<WoWGameModel> _logger;

    public WoWGameModel(ILogger<WoWGameModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("WoW Auto-Battle page visited");
    }
}
