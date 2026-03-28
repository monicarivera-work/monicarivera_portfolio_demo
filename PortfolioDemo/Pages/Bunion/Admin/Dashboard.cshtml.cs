using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion.Admin;

[Authorize(Policy = "BunionAdmin")]
public class DashboardModel : PageModel
{
    private readonly ArticleService _articles;
    public List<Article> Articles { get; private set; } = new();

    public DashboardModel(ArticleService articles)
    {
        _articles = articles;
    }

    public void OnGet()
    {
        Articles = _articles.GetAll(includeUnpublished: true);
    }
}
