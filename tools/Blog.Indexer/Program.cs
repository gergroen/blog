using Blog.Indexer;

// Usage:
//   index       --posts <dir> --out <wwwroot> --site-url <url> [--include-drafts]
//   postpublish --wwwroot <dir> --base-path </blog/> --site-url <url>
try
{
    var command = args.FirstOrDefault();
    var options = ParseOptions(args.Skip(1).ToArray());
    var siteUrl = WithTrailingSlash(Require(options, "site-url"));

    switch (command)
    {
        case "index":
        {
            var postsDir = Require(options, "posts");
            var outDir = Require(options, "out");
            var posts = PostIndexer.Load(postsDir, options.ContainsKey("include-drafts"));

            var changed = new[]
            {
                SiteFiles.WriteIfChanged(Path.Combine(postsDir, "index.json"), SiteFiles.IndexJson(posts)),
                SiteFiles.WriteIfChanged(Path.Combine(outDir, "sitemap.xml"), SiteFiles.Sitemap(posts, siteUrl)),
                SiteFiles.WriteIfChanged(Path.Combine(outDir, "feed.xml"), SiteFiles.Rss(posts, siteUrl)),
            }.Count(c => c);
            Console.WriteLine($"Blog index: {posts.Count} post(s), {changed} file(s) updated.");
            break;
        }
        case "postpublish":
            PublishProcessor.Run(Require(options, "wwwroot"), WithTrailingSlash(Require(options, "base-path")), siteUrl);
            break;
        default:
            throw new ArgumentException("Expected command 'index' or 'postpublish'.");
    }
    return 0;
}
catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException or IOException)
{
    // MSBuild-friendly error format so the build fails with a readable message.
    Console.Error.WriteLine($"Blog.Indexer : error BLOG001: {ex.Message}");
    return 1;
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Unexpected argument '{args[i]}'.");
        var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
        options[args[i][2..]] = hasValue ? args[++i] : "true";
    }
    return options;
}

static string Require(Dictionary<string, string> options, string name) =>
    options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"Missing --{name}.");

static string WithTrailingSlash(string value) => value.EndsWith('/') ? value : value + "/";
