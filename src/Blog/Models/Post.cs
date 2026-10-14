namespace MeuBlog.Models;

public record Post(
    string Slug,
    string Title,
    string Summary,
    DateTime Date,
    string ThemeName,
    string ThemeSlug,
    string Html);
