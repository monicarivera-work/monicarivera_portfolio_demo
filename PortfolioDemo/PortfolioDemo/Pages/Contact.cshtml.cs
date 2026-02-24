using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ContactModel : PageModel
{
    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    public string Message { get; set; } = string.Empty;

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // TODO: Implement email sending or save to database
        // For now, just redirect back with success message
        TempData["SuccessMessage"] = $"Thank you {Name}! Your message has been received.";
        return RedirectToPage();
    }
}