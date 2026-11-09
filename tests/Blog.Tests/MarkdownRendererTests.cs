using Blog.Core;

namespace Blog.Tests;

public class MarkdownRendererTests
{
    [Fact]
    public void Front_matter_is_not_rendered()
    {
        var html = MarkdownRenderer.ToHtml("---\ntitle: Secret\n---\n# Hello");
        Assert.DoesNotContain("Secret", html);
        Assert.Contains("Hello</h1>", html);
    }

    [Fact]
    public void Code_blocks_get_language_class()
    {
        var html = MarkdownRenderer.ToHtml("```csharp\nvar x = 1;\n```");
        Assert.Contains("class=\"language-csharp\"", html);
    }

    [Theory]
    [InlineData("![x](images/a.png)", "src=\"posts/images/a.png\"")]
    [InlineData("![x](./images/a.png)", "src=\"posts/images/a.png\"")]
    [InlineData("[x](https://example.com/images/a.png)", "href=\"https://example.com/images/a.png\"")]
    [InlineData("[x](post/other)", "href=\"post/other\"")]
    public void Image_links_are_rewritten_relative_to_posts(string markdown, string expected) =>
        Assert.Contains(expected, MarkdownRenderer.ToHtml(markdown));

    [Fact]
    public void Tables_are_supported() =>
        Assert.Contains("<table>", MarkdownRenderer.ToHtml("| a | b |\n|---|---|\n| 1 | 2 |"));

    [Fact]
    public void ReadingMinutes_is_at_least_one()
    {
        Assert.Equal(1, MarkdownRenderer.ReadingMinutes("short"));
        Assert.Equal(3, MarkdownRenderer.ReadingMinutes(string.Join(' ', Enumerable.Repeat("word", 600))));
    }
}
