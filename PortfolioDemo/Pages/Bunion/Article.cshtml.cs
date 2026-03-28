using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion;

public class ArticleModel : PageModel
{
    private readonly ArticleService _articles;

    public Article?       Article         { get; private set; }
    public List<Article>  RelatedArticles  { get; private set; } = new();
    public List<Article>  LatestArticles   { get; private set; } = new();

    public ArticleModel(ArticleService articles)
    {
        _articles = articles;
    }

    public IActionResult OnGet(string slug)
    {
        Article = _articles.GetBySlug(slug);
        if (Article == null) return Page(); // will show 404 view

        RelatedArticles = _articles
            .GetByCategory(Article.Category)
            .Where(a => a.Id != Article.Id)
            .Take(4)
            .ToList();

        LatestArticles = _articles
            .GetRecent(5)
            .Where(a => a.Id != Article.Id)
            .Take(4)
            .ToList();

        return Page();
    }
}
