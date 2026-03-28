using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion;

public class CategoryModel : PageModel
{
    private readonly ArticleService _articles;

    public string        Category { get; private set; } = string.Empty;
    public List<Article> Articles { get; private set; } = new();

    public CategoryModel(ArticleService articles)
    {
        _articles = articles;
    }

    public IActionResult OnGet(string category)
    {
        // Normalize to a valid category
        var match = BunionCategories.All.FirstOrDefault(c =>
            string.Equals(c, category, StringComparison.OrdinalIgnoreCase));

        if (match == null) return RedirectToPage("/Bunion/Index");

        Category = match;
        Articles = _articles.GetByCategory(Category);
        return Page();
    }
}
