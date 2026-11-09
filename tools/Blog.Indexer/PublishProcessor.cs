using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Blog.Core;
using static System.Net.WebUtility;

namespace Blog.Indexer;

/// <summary>
/// Post-processes the published wwwroot for GitHub Pages:
/// sets the base href, writes a static, prerendered HTML page per post (so deep links return 200 with real content),
/// and keeps the service worker's integrity hash of index.html in sync.
/// </summary>
public static partial class PublishProcessor
{
    [GeneratedRegex(@"<base\s+href=""[^""]*""\s*/?>")]
    private static partial Regex BaseTag();

    [GeneratedRegex(@"<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"<meta\s+name=""description""[^>]*>")]
    private static partial Regex DescriptionTag();

    [GeneratedRegex(@"(<div id=""app"">).*?(</div>)", RegexOptions.Singleline)]
    private static partial Regex AppDiv();

    public static void Run(string wwwroot, string basePath, string siteUrl)
    {
        var indexPath = Path.Combine(wwwroot, "index.html");
        var html = File.ReadAllText(indexPath);
        html = SetBaseHref(html, basePath);
        File.WriteAllText(indexPath, html);
        Console.WriteLine($"Base href set to {basePath}");

        var posts = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(wwwroot, "posts", "index.json")), BlogJsonContext.Default.ListPostMeta) ?? [];

        foreach (var post in posts)
        {
            var markdown = File.ReadAllText(Path.Combine(wwwroot, "posts", post.File));
            var article = $"""
                <article class="post prerendered">
                <h1>{HtmlEncode(post.Title)}</h1>
                <p class="post-info">{post.Date:yyyy-MM-dd}</p>
                {MarkdownRenderer.ToHtml(markdown)}
                </article>
                """;
            WritePage(wwwroot, $"post/{post.Slug}.html", html,
                $"{post.Title} - {SiteInfo.Title}", post.Summary is { Length: > 0 } s ? s : post.Title,
                SiteFiles.PostUrl(siteUrl, post), article, "article");
        }

        WritePage(wwwroot, "about.html", html, $"About - {SiteInfo.Title}", SiteInfo.Description, siteUrl + "about", null, "website");
        WritePage(wwwroot, "tags.html", html, $"Tags - {SiteInfo.Title}", SiteInfo.Description, siteUrl + "tags", null, "website");
        Console.WriteLine($"Wrote {posts.Count} prerendered post page(s)");

        UpdateServiceWorkerHash(Path.Combine(wwwroot, "service-worker-assets.js"), "index.html", File.ReadAllBytes(indexPath));
        Console.WriteLine("Updated service worker hash for index.html");

        // Without this, GitHub Pages runs Jekyll, which skips folders starting with "_" such as _framework.
        File.WriteAllText(Path.Combine(wwwroot, ".nojekyll"), "");
    }

    public static string SetBaseHref(string html, string basePath)
    {
        if (!BaseTag().IsMatch(html)) throw new InvalidOperationException("index.html has no <base href> tag.");
        return BaseTag().Replace(html, $"""<base href="{HtmlEncode(basePath)}" />""", 1);
    }

    public static string BuildPage(string html, string title, string description, string canonicalUrl, string? appContent, string ogType)
    {
        var head = $"""
                <link rel="canonical" href="{HtmlEncode(canonicalUrl)}" />
                <meta property="og:type" content="{ogType}" />
                <meta property="og:title" content="{HtmlEncode(title)}" />
                <meta property="og:description" content="{HtmlEncode(description)}" />
                <meta property="og:url" content="{HtmlEncode(canonicalUrl)}" />
            </head>
            """;
        html = TitleTag().Replace(html, $"<title>{HtmlEncode(title)}</title>", 1);
        html = DescriptionTag().Replace(html, $"""<meta name="description" content="{HtmlEncode(description)}" />""", 1);
        html = html.Replace("</head>", head, StringComparison.Ordinal);
        if (appContent is not null)
            html = AppDiv().Replace(html, m => m.Groups[1].Value + appContent + m.Groups[2].Value, 1);
        return html;
    }

    /// <summary>Replaces the sha256 integrity hash of one asset in service-worker-assets.js.</summary>
    public static void UpdateServiceWorkerHash(string assetsJsPath, string assetUrl, byte[] content)
    {
        const string prefix = "self.assetsManifest = ";
        var js = File.ReadAllText(assetsJsPath).Trim();
        if (!js.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected service-worker-assets.js format.");

        var manifest = JsonNode.Parse(js[prefix.Length..].TrimEnd(';'))!;
        var asset = manifest["assets"]!.AsArray().FirstOrDefault(a => (string?)a!["url"] == assetUrl)
            ?? throw new InvalidOperationException($"{assetUrl} not found in service worker assets.");
        asset["hash"] = "sha256-" + Convert.ToBase64String(SHA256.HashData(content));

        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(assetsJsPath, prefix + manifest.ToJsonString(options) + ";\n");
    }

    private static void WritePage(string wwwroot, string relativePath, string html, string title, string description,
        string canonicalUrl, string? appContent, string ogType)
    {
        var path = Path.Combine(wwwroot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, BuildPage(html, title, description, canonicalUrl, appContent, ogType));
    }
}
