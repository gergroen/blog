using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Blog.Core;
using Blog.Indexer;

namespace Blog.Tests;

public class SiteFilesTests
{
    private const string SiteUrl = "https://example.github.io/blog/";

    private static readonly List<PostMeta> Posts =
    [
        new() { Slug = "second", Title = "Second & last", Date = new(2026, 2, 1), File = "2026-02-01-second.md", Tags = ["C#"], Summary = "S2" },
        new() { Slug = "first", Title = "First", Date = new(2026, 1, 1), File = "2026-01-01-first.md" },
    ];

    [Fact]
    public void IndexJson_round_trips()
    {
        var json = SiteFiles.IndexJson(Posts);
        Assert.Contains("\"slug\": \"second\"", json);
        Assert.Contains("\"date\": \"2026-02-01\"", json);
        Assert.Equal(Posts, JsonSerializer.Deserialize(json, BlogJsonContext.Default.ListPostMeta), new PostComparer());
    }

    [Fact]
    public void Sitemap_contains_post_urls()
    {
        var doc = XDocument.Parse(SiteFiles.Sitemap(Posts, SiteUrl));
        var locs = doc.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value).ToList();
        Assert.Contains(SiteUrl, locs);
        Assert.Contains(SiteUrl + "post/second", locs);
        Assert.Contains(SiteUrl + "post/first", locs);
    }

    [Fact]
    public void Rss_contains_items_with_escaped_titles()
    {
        var doc = XDocument.Parse(SiteFiles.Rss(Posts, SiteUrl));
        var items = doc.Descendants("item").ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Second & last", items[0].Element("title")!.Value);
        Assert.Equal(SiteUrl + "post/second", items[0].Element("link")!.Value);
        Assert.Equal("C#", items[0].Element("category")!.Value);
    }

    [Fact]
    public void SetBaseHref_replaces_base_tag()
    {
        var html = PublishProcessor.SetBaseHref("<head><base href=\"/\" /></head>", "/blog/");
        Assert.Equal("<head><base href=\"/blog/\" /></head>", html);
    }

    [Fact]
    public void BuildPage_sets_title_meta_and_prerendered_content()
    {
        const string html = """
            <html><head><title>Site</title><meta name="description" content="Site description" /></head>
            <body><div id="app"><p class="app-loading">Loading…</p></div></body></html>
            """;

        var page = PublishProcessor.BuildPage(html, "A & B", "Desc", SiteUrl + "post/a", "<article>Hi</article>", "article");

        Assert.Contains("<title>A &amp; B</title>", page);
        Assert.Contains("<meta name=\"description\" content=\"Desc\" />", page);
        Assert.Contains($"<link rel=\"canonical\" href=\"{SiteUrl}post/a\" />", page);
        Assert.Contains("<div id=\"app\"><article>Hi</article></div>", page);
        Assert.DoesNotContain("Loading…", page);
    }

    [Fact]
    public void UpdateServiceWorkerHash_updates_matching_asset()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                self.assetsManifest = {
                  "version": "abc",
                  "assets": [
                    { "hash": "sha256-old", "url": "index.html" },
                    { "hash": "sha256-other", "url": "css/app.css" }
                  ]
                };
                """);
            byte[] content = [1, 2, 3];

            PublishProcessor.UpdateServiceWorkerHash(path, "index.html", content);

            var js = File.ReadAllText(path);
            Assert.StartsWith("self.assetsManifest = ", js);
            Assert.Contains("sha256-" + Convert.ToBase64String(SHA256.HashData(content)), js);
            Assert.Contains("sha256-other", js);
            Assert.DoesNotContain("sha256-old", js);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class PostComparer : IEqualityComparer<PostMeta>
    {
        public bool Equals(PostMeta? x, PostMeta? y) =>
            x is not null && y is not null && x.Slug == y.Slug && x.Title == y.Title && x.Date == y.Date &&
            x.File == y.File && x.Summary == y.Summary && x.Tags.SequenceEqual(y.Tags);

        public int GetHashCode(PostMeta obj) => obj.Slug.GetHashCode();
    }
}
