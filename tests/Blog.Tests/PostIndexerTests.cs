using Blog.Indexer;

namespace Blog.Tests;

public sealed class PostIndexerTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("blog-tests-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void AddPost(string fileName, string content) => File.WriteAllText(Path.Combine(_dir, fileName), content);

    [Fact]
    public void Parse_reads_front_matter()
    {
        var post = PostIndexer.Parse("2026-11-09-hello.md", """
            ---
            title: "Hello: world"
            tags: [.NET, C#]
            summary: A summary
            ---
            Some *content*.
            """, includeDrafts: false);

        Assert.NotNull(post);
        Assert.Equal("hello", post.Slug);
        Assert.Equal("Hello: world", post.Title);
        Assert.Equal(new DateOnly(2026, 11, 9), post.Date);
        Assert.Equal([".NET", "C#"], post.Tags);
        Assert.Equal("A summary", post.Summary);
        Assert.Equal("2026-11-09-hello.md", post.File);
        Assert.Equal(1, post.ReadingMinutes);
    }

    [Fact]
    public void Front_matter_date_overrides_file_name_date()
    {
        var post = PostIndexer.Parse("2026-11-09-hello.md", "---\ntitle: T\ndate: 2025-01-02\n---\n", false);
        Assert.Equal(new DateOnly(2025, 1, 2), post!.Date);
    }

    [Fact]
    public void Drafts_are_skipped_unless_included()
    {
        const string text = "---\ntitle: Draft\ndraft: true\n---\nWork in progress";
        Assert.Null(PostIndexer.Parse("2026-11-09-draft.md", text, includeDrafts: false));
        Assert.NotNull(PostIndexer.Parse("2026-11-09-draft.md", text, includeDrafts: true));
    }

    [Theory]
    [InlineData("no front matter")]
    [InlineData("---\ntitle: Unclosed\n")]
    [InlineData("---\nsummary: no title\n---\n")]
    public void Invalid_posts_throw(string text) =>
        Assert.Throws<FormatException>(() => PostIndexer.Parse("2026-11-09-x.md", text, false));

    [Fact]
    public void Missing_date_throws() =>
        Assert.Throws<FormatException>(() => PostIndexer.Parse("undated.md", "---\ntitle: T\n---\n", false));

    [Fact]
    public void Load_orders_newest_first_and_skips_drafts()
    {
        AddPost("2026-01-01-old.md", "---\ntitle: Old\n---\n");
        AddPost("2026-03-01-new.md", "---\ntitle: New\n---\n");
        AddPost("2026-02-01-draft.md", "---\ntitle: Draft\ndraft: true\n---\n");

        var posts = PostIndexer.Load(_dir, includeDrafts: false);

        Assert.Equal(["new", "old"], posts.Select(p => p.Slug));
    }

    [Fact]
    public void Load_reports_file_name_on_error()
    {
        AddPost("2026-01-01-broken.md", "---\ntitle: [unclosed\n---\n");
        var ex = Assert.Throws<FormatException>(() => PostIndexer.Load(_dir, false));
        Assert.Contains("2026-01-01-broken.md", ex.Message);
    }

    [Fact]
    public void Load_rejects_duplicate_slugs()
    {
        AddPost("2026-01-01-same.md", "---\ntitle: A\n---\n");
        AddPost("2026-02-01-same.md", "---\ntitle: B\n---\n");
        Assert.Throws<FormatException>(() => PostIndexer.Load(_dir, false));
    }
}
