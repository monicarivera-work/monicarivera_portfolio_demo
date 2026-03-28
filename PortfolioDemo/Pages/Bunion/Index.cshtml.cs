using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion;

public class IndexModel : PageModel
{
    private readonly ArticleService _articles;

    public List<Article> FeaturedArticles { get; private set; } = new();
    public List<Article> RecentArticles   { get; private set; } = new();
    public List<Article> AllArticles      { get; private set; } = new();

    public IndexModel(ArticleService articles)
    {
        _articles = articles;
    }

    public void OnGet()
    {
        AllArticles      = _articles.GetAll();
        FeaturedArticles = _articles.GetFeatured();
        // If no articles are marked featured, promote the first two
        if (!FeaturedArticles.Any() && AllArticles.Any())
            FeaturedArticles = AllArticles.Take(2).ToList();

        RecentArticles = AllArticles
            .Where(a => !FeaturedArticles.Take(1).Select(f => f.Id).Contains(a.Id))
            .Take(6)
            .ToList();
    }
}
