namespace PortfolioDemo.Models.Bunion;

public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Author { get; set; } = "The Bunion Staff";
    public string Category { get; set; } = "News";
    public List<string> Tags { get; set; } = new();
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; } = true;
}

public static class BunionCategories
{
    public static readonly string[] All =
    [
        "News", "Politics", "World", "Science", "Health", "Business", "Sports", "Entertainment", "Opinion"
    ];
}
