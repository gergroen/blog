using System.Globalization;
using Blog.Core;

namespace Blog.Indexer;

public static class PostIndexer
{
    /// <summary>Reads all <c>*.md</c> posts in a folder, newest first.</summary>
    public static List<PostMeta> Load(string postsDirectory, bool includeDrafts)
    {
        var posts = new List<PostMeta>();
        foreach (var path in Directory.EnumerateFiles(postsDirectory, "*.md"))
        {
            var fileName = Path.GetFileName(path);
            try
            {
                var post = Parse(fileName, File.ReadAllText(path), includeDrafts);
                if (post is not null) posts.Add(post);
            }
            catch (Exception ex) when (ex is FormatException or YamlDotNet.Core.YamlException)
            {
                throw new FormatException($"{fileName}: {ex.Message}", ex);
            }
        }

        var duplicate = posts.GroupBy(p => p.Slug).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new FormatException($"Duplicate slug '{duplicate.Key}': {string.Join(", ", duplicate.Select(p => p.File))}");

        return [.. posts.OrderByDescending(p => p.Date).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>Returns null for drafts that should not be published.</summary>
    public static PostMeta? Parse(string fileName, string text, bool includeDrafts)
    {
        var (frontMatter, body) = FrontMatter.Parse(text);
        if (frontMatter.Draft && !includeDrafts) return null;

        if (string.IsNullOrWhiteSpace(frontMatter.Title))
            throw new FormatException("Front matter must contain a 'title'.");

        var date = frontMatter.Date is { Length: > 0 } d
            ? DateOnly.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            : Slugs.DateFromFileName(fileName)
              ?? throw new FormatException("Add a 'date' (yyyy-MM-dd) or name the file 'yyyy-MM-dd-slug.md'.");

        return new PostMeta
        {
            Slug = Slugs.FromFileName(fileName),
            Title = frontMatter.Title.Trim(),
            Date = date,
            Tags = [.. frontMatter.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)],
            Summary = frontMatter.Summary?.Trim() ?? "",
            File = fileName,
            ReadingMinutes = MarkdownRenderer.ReadingMinutes(body),
        };
    }
}
