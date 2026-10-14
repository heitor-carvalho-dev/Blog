namespace MeuBlog.Models;

// ImageUrl is null when the theme has no image (the card falls back to a default background)
public record Theme(string Slug, string Name, int TotalPosts, string? ImageUrl);
