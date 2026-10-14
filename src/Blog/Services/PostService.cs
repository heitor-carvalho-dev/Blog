using System.Globalization;
using System.Text;
using Markdig;
using MeuBlog.Models;

namespace MeuBlog.Services;

public class PostService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".avif", ".svg"];

    private readonly List<Post> _posts;
    private readonly string? _themeImagesFolder;

    public PostService(IWebHostEnvironment env)
    {
        // Theme images: wwwroot/img/themes/{Capitalized-Theme-Slug}.jpg (or png, webp, avif, svg)
        if (env.WebRootPath is not null)
            _themeImagesFolder = Path.Combine(env.WebRootPath, "img", "themes");

        var folder = Path.Combine(env.ContentRootPath, "Content", "Posts");

        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // tables, task lists, footnotes, etc.
            .Build();

        _posts = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.md")
                .Select(file => Parse(file, pipeline))
                .OrderByDescending(p => p.Date)
                .ToList()
            : new List<Post>();
    }

    // ---------- Posts ----------

    public IReadOnlyList<Post> GetAll() => _posts;

    public Post? GetBySlug(string slug) =>
        _posts.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    // ---------- Themes ----------

    // Themes are discovered from the "theme" field of each post
    public IReadOnlyList<Theme> GetThemes() =>
        _posts
            .GroupBy(p => p.ThemeSlug)
            .Select(g => new Theme(g.Key, g.First().ThemeName, g.Count(), FindThemeImage(g.Key)))
            .OrderBy(t => t.Name)
            .ToList();

    public Theme? GetTheme(string themeSlug) =>
        GetThemes().FirstOrDefault(t => t.Slug.Equals(themeSlug, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Post> GetByTheme(string themeSlug) =>
        _posts
            .Where(p => p.ThemeSlug.Equals(themeSlug, StringComparison.OrdinalIgnoreCase))
            .ToList();

    // Image file names start with an uppercase letter:
    //   theme "database"        -> Database.jpg
    //   theme "Web Development" -> Web-Development.jpg
    private string? FindThemeImage(string themeSlug)
    {
        if (_themeImagesFolder is null || !Directory.Exists(_themeImagesFolder))
            return null;

        var fileName = ToImageFileName(themeSlug);

        foreach (var ext in ImageExtensions)
        {
            if (File.Exists(Path.Combine(_themeImagesFolder, fileName + ext)))
                return $"/img/themes/{fileName}{ext}";
        }
        return null;
    }

    // "web-development" -> "Web-Development"
    private static string ToImageFileName(string slug) =>
        string.Join("-", slug
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    // ---------- Reading the .md files ----------

    // Expected file format:
    // ---
    // title: My title
    // theme: C# and .NET
    // date: 2026-10-13
    // summary: Short summary
    // ---
    // Markdown content...
    private static Post Parse(string path, MarkdownPipeline pipeline)
    {
        var slug = Path.GetFileNameWithoutExtension(path);
        var text = File.ReadAllText(path).Replace("\r\n", "\n");

        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var body = text;

        if (text.StartsWith("---\n"))
        {
            var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end > 0)
            {
                foreach (var line in text[4..end].Split('\n'))
                {
                    var i = line.IndexOf(':');
                    if (i > 0)
                        meta[line[..i].Trim()] = line[(i + 1)..].Trim();
                }
                body = text[(end + 4)..].TrimStart('\n');
            }
        }

        meta.TryGetValue("title", out var title);
        meta.TryGetValue("summary", out var summary);
        meta.TryGetValue("theme", out var theme);

        var themeName = string.IsNullOrWhiteSpace(theme) ? "General" : theme;

        var date = meta.TryGetValue("date", out var d)
                   && DateTime.TryParse(d, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : File.GetCreationTime(path);

        return new Post(
            slug,
            title ?? slug,
            summary ?? "",
            date,
            themeName,
            Slugify(themeName),
            Markdown.ToHtml(body, pipeline));
    }

    // "Web Development" -> "web-development"
    private static string Slugify(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue; // strip accents

            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
