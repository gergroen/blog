using Blog.Core;

namespace Blog.Tests;

public class SlugsTests
{
    [Theory]
    [InlineData("2026-11-09-hello-world.md", "hello-world")]
    [InlineData("2026-11-09-Hello World!.md", "hello-world")]
    [InlineData("no-date.md", "no-date")]
    public void FromFileName_strips_date_and_extension(string fileName, string expected) =>
        Assert.Equal(expected, Slugs.FromFileName(fileName));

    [Fact]
    public void DateFromFileName_parses_prefix() =>
        Assert.Equal(new DateOnly(2026, 11, 9), Slugs.DateFromFileName("2026-11-09-hello.md"));

    [Theory]
    [InlineData("no-date.md")]
    [InlineData("2026-13-40-bad.md")]
    public void DateFromFileName_returns_null_without_valid_date(string fileName) =>
        Assert.Null(Slugs.DateFromFileName(fileName));

    [Theory]
    [InlineData("ASP.NET Core", "asp-net-core")]
    [InlineData(".NET", "net")]
    [InlineData("C#", "csharp")]
    [InlineData("GitHub Pages", "github-pages")]
    [InlineData("Café", "cafe")]
    public void ForTag_is_url_safe(string tag, string expected) =>
        Assert.Equal(expected, Slugs.ForTag(tag));
}
