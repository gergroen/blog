using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Blog.Indexer;

/// <summary>YAML block at the top of a post, between two "---" lines.</summary>
public sealed class FrontMatter
{
    public string? Title { get; set; }
    public string? Date { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? Summary { get; set; }
    public bool Draft { get; set; }

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>Splits a post into its front matter and Markdown body.</summary>
    public static (FrontMatter FrontMatter, string Body) Parse(string text)
    {
        using var reader = new StringReader(text.TrimStart('﻿'));
        if (reader.ReadLine()?.Trim() != "---")
            throw new FormatException("Post must start with a '---' front matter block.");

        var yaml = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) is not null && line.Trim() != "---")
            yaml.Add(line);
        if (line is null)
            throw new FormatException("Front matter block is not closed with '---'.");

        var frontMatter = Deserializer.Deserialize<FrontMatter?>(string.Join('\n', yaml)) ?? new FrontMatter();
        return (frontMatter, reader.ReadToEnd());
    }
}
