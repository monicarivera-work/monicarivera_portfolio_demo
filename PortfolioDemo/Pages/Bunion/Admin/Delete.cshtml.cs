using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioDemo.Services.Bunion;

namespace PortfolioDemo.Pages.Bunion.Admin;

[Authorize(Policy = "BunionAdmin")]
[ValidateAntiForgeryToken]
public class DeleteModel : PageModel
{
    private readonly ArticleService _articles;

    public DeleteModel(ArticleService articles)
    {
        _articles = articles;
    }

    /// <summary>POST-based delete with CSRF protection.</summary>
    public IActionResult OnPost(Guid id)
    {
        var article = _articles.GetById(id);
        if (article == null) return RedirectToPage("/Bunion/Admin/Dashboard");

        var deleted = _articles.Delete(id);
        if (deleted)
            TempData["Success"] = $"Article \"{article.Title}\" deleted.";

        return RedirectToPage("/Bunion/Admin/Dashboard");
    }
}
