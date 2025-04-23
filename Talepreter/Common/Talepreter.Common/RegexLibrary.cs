using System.Text.RegularExpressions;

namespace Talepreter.Common;

public static class RegexExtensions
{
    public static string? GetMatchOnce(this Regex regex, string? value, bool throwExc = true)
    {
        if (value != null)
        {
            var matches = regex.Matches(value);
            if (matches.Count == 0) return null;
            if (matches.Count == 1) return matches[0].Value.Trim();
        }
        if (throwExc) throw new InvalidOperationException("Regex pattern matches more than once or none but expected only once");
        return null;
    }
    public static string ExpectMatchOnce(this Regex regex, string value, bool throwExc = true)
    {
        var matches = regex.Matches(value);
        if (matches.Count == 1) return matches[0].Value.Trim();
        if (throwExc) throw new InvalidOperationException("Regex pattern matches more than once or none but expected only once");
        return default!;
    }
    public static string? GetMatchOnce(this Regex regex, string? value, string? @default)
    {
        if (value != null)
        {
            var matches = regex.Matches(value);
            if (matches.Count == 0) return @default;
            if (matches.Count == 1) return matches[0].Value.Trim();
        }
        return @default;
    }
}
