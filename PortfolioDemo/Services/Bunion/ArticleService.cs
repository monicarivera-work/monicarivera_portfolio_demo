using System.Text.Json;
using PortfolioDemo.Models.Bunion;

namespace PortfolioDemo.Services.Bunion;

public class ArticleService
{
    private readonly string _dataFilePath;
    private readonly ILogger<ArticleService> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ArticleService(IWebHostEnvironment env, ILogger<ArticleService> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _dataFilePath = Path.Combine(dataDir, "articles.json");
        SeedIfEmpty();
    }

    // ── Read ────────────────────────────────────────────────────────────────

    public List<Article> GetAll(bool includeUnpublished = false)
    {
        var articles = Load();
        return includeUnpublished
            ? articles.OrderByDescending(a => a.PublishDate).ToList()
            : articles.Where(a => a.IsPublished).OrderByDescending(a => a.PublishDate).ToList();
    }

    public Article? GetById(Guid id) =>
        Load().FirstOrDefault(a => a.Id == id);

    public Article? GetBySlug(string slug) =>
        Load().FirstOrDefault(a => a.Slug == slug && a.IsPublished);

    public List<Article> GetByCategory(string category, bool includeUnpublished = false)
    {
        var articles = Load();
        return articles
            .Where(a => string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase)
                        && (includeUnpublished || a.IsPublished))
            .OrderByDescending(a => a.PublishDate)
            .ToList();
    }

    public List<Article> GetFeatured() =>
        Load().Where(a => a.IsFeatured && a.IsPublished).OrderByDescending(a => a.PublishDate).ToList();

    public List<Article> GetRecent(int count = 6) =>
        GetAll().Take(count).ToList();

    // ── Write ───────────────────────────────────────────────────────────────

    public Article Create(Article article)
    {
        article.Id = Guid.NewGuid();
        article.Slug = GenerateSlug(article.Title, article.Id);
        article.PublishDate = DateTime.UtcNow;

        _lock.EnterWriteLock();
        try
        {
            var articles = LoadUnsafe();
            articles.Add(article);
            SaveUnsafe(articles);
        }
        finally { _lock.ExitWriteLock(); }

        return article;
    }

    public bool Update(Article updated)
    {
        _lock.EnterWriteLock();
        try
        {
            var articles = LoadUnsafe();
            var idx = articles.FindIndex(a => a.Id == updated.Id);
            if (idx < 0) return false;

            // Preserve original publish date and slug
            updated.PublishDate = articles[idx].PublishDate;
            if (string.IsNullOrWhiteSpace(updated.Slug))
                updated.Slug = articles[idx].Slug;

            articles[idx] = updated;
            SaveUnsafe(articles);
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public bool Delete(Guid id)
    {
        _lock.EnterWriteLock();
        try
        {
            var articles = LoadUnsafe();
            var removed = articles.RemoveAll(a => a.Id == id);
            if (removed == 0) return false;
            SaveUnsafe(articles);
            return true;
        }
        finally { _lock.ExitWriteLock(); }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    public static string GenerateSlug(string title, Guid id)
    {
        var slug = title.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        if (slug.Length > 80) slug = slug[..80].TrimEnd('-');
        return $"{slug}-{id.ToString()[..8]}";
    }

    private List<Article> Load()
    {
        _lock.EnterReadLock();
        try { return LoadUnsafe(); }
        finally { _lock.ExitReadLock(); }
    }

    private List<Article> LoadUnsafe()
    {
        try
        {
            if (!File.Exists(_dataFilePath)) return new List<Article>();
            var json = File.ReadAllText(_dataFilePath);
            return JsonSerializer.Deserialize<List<Article>>(json, _jsonOptions) ?? new List<Article>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load articles from {Path}", _dataFilePath);
            return new List<Article>();
        }
    }

    /// <summary>Write atomically: serialise to a temp file then replace the target.</summary>
    private void SaveUnsafe(List<Article> articles)
    {
        var tmpPath = _dataFilePath + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(articles, _jsonOptions);
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, _dataFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save articles to {Path}", _dataFilePath);
            // Clean up orphaned tmp file if present
            if (File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { /* best-effort */ }
            }
        }
    }

    private void SeedIfEmpty()
    {
        // Seed only when no articles.json exists yet
        if (File.Exists(_dataFilePath) && new FileInfo(_dataFilePath).Length > 0) return;

        var seedPath = Path.Combine(Path.GetDirectoryName(_dataFilePath)!, "articles.seed.json");
        if (File.Exists(seedPath))
        {
            File.Copy(seedPath, _dataFilePath, overwrite: true);
        }
    }
}
