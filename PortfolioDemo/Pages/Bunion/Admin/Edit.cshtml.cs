using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion.Admin;

[Authorize(Policy = "BunionAdmin")]
public class EditModel : PageModel
{
    private readonly ArticleService _articles;
    public Article? Article       { get; private set; }
    public string   ErrorMessage  { get; private set; } = string.Empty;

    public EditModel(ArticleService articles)
    {
        _articles = articles;
    }

    public IActionResult OnGet(Guid id)
    {
        Article = _articles.GetById(id);
        if (Article == null) return NotFound();
        return Page();
    }

    public IActionResult OnPost(
        Guid id,
        string title, string subtitle, string body,
        string category, string author,
        string? imageUrl, string? imageAlt,
        string? tags,
        bool isFeatured, bool isPublished)
    {
        Article = _articles.GetById(id);
        if (Article == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            ErrorMessage = "Title and Body are required.";
            return Page();
        }

        Article.Title       = title.Trim();
        Article.Subtitle    = subtitle?.Trim() ?? "";
        Article.Body        = body;
        Article.Category    = category;
        Article.Author      = string.IsNullOrWhiteSpace(author) ? "The Bunion Staff" : author.Trim();
        Article.ImageUrl    = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Article.ImageAlt    = string.IsNullOrWhiteSpace(imageAlt) ? null : imageAlt.Trim();
        Article.Tags        = ParseTags(tags);
        Article.IsFeatured  = isFeatured;
        Article.IsPublished = isPublished;

        _articles.Update(Article);
        TempData["Success"] = $"Article \"{Article.Title}\" updated successfully.";
        return RedirectToPage("/Bunion/Admin/Dashboard");
    }

    private static List<string> ParseTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? new List<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
