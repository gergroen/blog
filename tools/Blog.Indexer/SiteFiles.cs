using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Blog.Core;

namespace Blog.Indexer;

/// <summary>Generates index.json, sitemap.xml and feed.xml.</summary>
public static class SiteFiles
{
    public static string IndexJson(List<PostMeta> posts) =>
        JsonSerializer.Serialize(posts, BlogJsonContext.Default.ListPostMeta);

    public static string Sitemap(IEnumerable<PostMeta> posts, string siteUrl)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new[] { (Loc: siteUrl, LastMod: (DateOnly?)null), (siteUrl + "about", null), (siteUrl + "tags", null) }
            .Concat(posts.Select(p => (Loc: PostUrl(siteUrl, p), LastMod: (DateOnly?)p.Date)));
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset",
                urls.Select(u => new XElement(ns + "url",
                    new XElement(ns + "loc", u.Loc),
                    u.LastMod is { } d ? new XElement(ns + "lastmod", d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : null))));
        return ToXmlString(doc);
    }

    public static string Rss(IEnumerable<PostMeta> posts, string siteUrl)
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var items = posts.Take(20).ToList();
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null),
            new XElement("rss", new XAttribute("version", "2.0"), new XAttribute(XNamespace.Xmlns + "atom", atom),
                new XElement("channel",
                    new XElement("title", SiteInfo.Title),
                    new XElement("link", siteUrl),
                    new XElement("description", SiteInfo.Description),
                    new XElement("language", "en"),
                    new XElement(atom + "link", new XAttribute("href", siteUrl + "feed.xml"),
                        new XAttribute("rel", "self"), new XAttribute("type", "application/rss+xml")),
                    items.Count > 0 ? new XElement("lastBuildDate", Rfc822(items[0].Date)) : null,
                    items.Select(p => new XElement("item",
                        new XElement("title", p.Title),
                        new XElement("link", PostUrl(siteUrl, p)),
                        new XElement("guid", new XAttribute("isPermaLink", "true"), PostUrl(siteUrl, p)),
                        new XElement("pubDate", Rfc822(p.Date)),
                        new XElement("description", p.Summary),
                        p.Tags.Select(t => new XElement("category", t)))))));
        return ToXmlString(doc);
    }

    public static string PostUrl(string siteUrl, PostMeta post) => $"{siteUrl}post/{post.Slug}";

    /// <summary>Writes the file only when its content changed, so <c>dotnet watch</c> does not loop.</summary>
    public static bool WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path) == content) return false;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return true;
    }

    private static string Rfc822(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToString("r", CultureInfo.InvariantCulture);

    private static string ToXmlString(XDocument doc)
    {
        using var writer = new Utf8StringWriter();
        doc.Save(writer);
        return writer.ToString();
    }

    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}
