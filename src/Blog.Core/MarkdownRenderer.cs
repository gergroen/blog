using Markdig;
using Markdig.Renderers;

namespace Blog.Core;

/// <summary>Renders post Markdown to HTML. Used by the app at runtime and by the build tooling for prerendering.</summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .Build();

    public static string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);
        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer) { LinkRewriter = RewriteLink };
        Pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    /// <summary>
    /// Posts reference images as "images/x.png" (relative to the Markdown file, so they also preview on GitHub).
    /// The page's base href is the site root, so those links are rewritten to "posts/images/x.png".
    /// </summary>
    public static string RewriteLink(string url)
    {
        if (url.StartsWith("./", StringComparison.Ordinal)) url = url[2..];
        return url.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ? "posts/" + url : url;
    }

    public static int ReadingMinutes(string markdown)
    {
        var words = markdown.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(words / 200.0));
    }
}
