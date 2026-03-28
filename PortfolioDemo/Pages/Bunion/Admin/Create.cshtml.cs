using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Models.Bunion;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion.Admin;

[Authorize(Policy = "BunionAdmin")]
public class CreateModel : PageModel
{
    private readonly ArticleService _articles;
    public Article Input { get; set; } = new() { Author = "The Bunion Staff", IsPublished = true };
    public string ErrorMessage { get; private set; } = string.Empty;

    public CreateModel(ArticleService articles)
    {
        _articles = articles;
    }

    public void OnGet() { }

    public IActionResult OnPost(
        string title, string subtitle, string body,
        string category, string author,
        string? imageUrl, string? imageAlt,
        string? tags,
        bool isFeatured, bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            ErrorMessage = "Title and Body are required.";
            Input = BuildInput(title, subtitle, body, category, author, imageUrl, imageAlt, tags, isFeatured, isPublished);
            return Page();
        }

        var article = new Article
        {
            Title       = title.Trim(),
            Subtitle    = subtitle?.Trim() ?? "",
            Body        = body,
            Category    = category,
            Author      = string.IsNullOrWhiteSpace(author) ? "The Bunion Staff" : author.Trim(),
            ImageUrl    = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            ImageAlt    = string.IsNullOrWhiteSpace(imageAlt) ? null : imageAlt.Trim(),
            Tags        = ParseTags(tags),
            IsFeatured  = isFeatured,
            IsPublished = isPublished
        };

        _articles.Create(article);
        TempData["Success"] = $"Article \"{article.Title}\" created successfully.";
        return RedirectToPage("/Bunion/Admin/Dashboard");
    }

    private Article BuildInput(
        string title, string subtitle, string body,
        string category, string author,
        string? imageUrl, string? imageAlt,
        string? tags, bool isFeatured, bool isPublished) =>
        new()
        {
            Title       = title,
            Subtitle    = subtitle,
            Body        = body,
            Category    = category,
            Author      = author,
            ImageUrl    = imageUrl,
            ImageAlt    = imageAlt,
            Tags        = ParseTags(tags),
            IsFeatured  = isFeatured,
            IsPublished = isPublished
        };

    private static List<string> ParseTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? new List<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
