using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioDemo.Pages;

public class StudyModel : PageModel
{
    private readonly ILogger<StudyModel> _logger;

    public StudyModel(ILogger<StudyModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        _logger.LogInformation("Study page visited");
    }
}
