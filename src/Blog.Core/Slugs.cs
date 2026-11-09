using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Blog.Core;

public static partial class Slugs
{
    [GeneratedRegex(@"^(?<date>\d{4}-\d{2}-\d{2})-(?<slug>.+)$")]
    private static partial Regex DatedFileName();

    /// <summary>"2026-11-09-hello-world.md" becomes "hello-world".</summary>
    public static string FromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var match = DatedFileName().Match(name);
        return Slugify(match.Success ? match.Groups["slug"].Value : name);
    }

    /// <summary>Returns the date prefix of "2026-11-09-hello-world.md", or null if there is none.</summary>
    public static DateOnly? DateFromFileName(string fileName)
    {
        var match = DatedFileName().Match(Path.GetFileNameWithoutExtension(fileName));
        return match.Success && DateOnly.TryParseExact(match.Groups["date"].Value, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    /// <summary>URL-safe slug for a tag, e.g. "ASP.NET Core" becomes "asp-net-core".</summary>
    public static string ForTag(string tag) => Slugify(tag);

    public static string Slugify(string value)
    {
        var sb = new StringBuilder(value.Length);
        var pendingDash = false;
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingDash && sb.Length > 0) sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
                pendingDash = false;
            }
            else if (c == '#' && sb.Length > 0 && sb[^1] == 'c')
            {
                sb.Append("sharp"); // C# -> csharp
            }
            else
            {
                pendingDash = true;
            }
        }
        return sb.ToString();
    }
}
