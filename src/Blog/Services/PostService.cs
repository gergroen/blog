using System.Net.Http.Json;
using Blog.Core;

namespace Blog.Services;

/// <summary>Loads the post index and post content. Results are cached for the lifetime of the app.</summary>
public sealed class PostService(HttpClient http)
{
    private Task<List<PostMeta>>? _posts;
    private readonly Dictionary<string, Task<string>> _html = [];

    public Task<List<PostMeta>> GetPostsAsync() => _posts ??= LoadIndexAsync();

    public async Task<PostMeta?> GetPostAsync(string slug) =>
        (await GetPostsAsync()).FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public Task<string> GetHtmlAsync(PostMeta post)
    {
        if (!_html.TryGetValue(post.Slug, out var task) || task.IsFaulted)
            _html[post.Slug] = task = LoadHtmlAsync(post);
        return task;
    }

    /// <summary>All tags with their post count, most used first.</summary>
    public async Task<List<(string Tag, int Count)>> GetTagsAsync() =>
        [.. (await GetPostsAsync())
            .SelectMany(p => p.Tags)
            .GroupBy(Slugs.ForTag)
            .Select(g => (Tag: g.First(), Count: g.Count()))
            .OrderByDescending(t => t.Count).ThenBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)];

    private async Task<List<PostMeta>> LoadIndexAsync()
    {
        try
        {
            return await http.GetFromJsonAsync("posts/index.json", BlogJsonContext.Default.ListPostMeta) ?? [];
        }
        catch
        {
            _posts = null; // allow a retry, e.g. after coming back online
            throw;
        }
    }

    private async Task<string> LoadHtmlAsync(PostMeta post)
    {
        var markdown = await http.GetStringAsync($"posts/{Uri.EscapeDataString(post.File)}");
        return MarkdownRenderer.ToHtml(markdown);
    }
}
