using System.Text.Json.Serialization;

namespace Blog.Core;

/// <summary>Metadata of a published post, as stored in <c>posts/index.json</c>.</summary>
public sealed record PostMeta
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required DateOnly Date { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string Summary { get; init; } = "";

    /// <summary>File name of the Markdown source, relative to the <c>posts/</c> folder.</summary>
    public required string File { get; init; }

    public int ReadingMinutes { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(List<PostMeta>))]
public partial class BlogJsonContext : JsonSerializerContext;
