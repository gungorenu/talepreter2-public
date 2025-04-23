namespace Talepreter.Common;

public static class ConversionExtensions
{
    public static int ToInt(this string s) => int.Parse(s);
    public static ushort ToUShort(this string s) => ushort.Parse(s);
    public static int TryToInt(this string s, int @default = 0)
    {
        if (int.TryParse(s, out var v)) return v;
        return @default;
    }
    public static long ToLong(this string s) => long.Parse(s);
    public static bool ToBool(this string s) => bool.Parse(s);
    public static T[] SplitInto<T>(this string s, string separator, Func<string, T> converter)
        => s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => converter(x)).ToArray();
    public static string MergeInto<T>(this T[] s, string separator, Func<T, string> converter)
        => s.Aggregate("", (a, b) => $"{a}{separator}{converter(b)}").Trim(separator.ToCharArray());
    public static string[] SplitInto(this string s, string separator)
        => s.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
    public static string MergeInto(this string[] s, string separator)
        => s.Aggregate("", (a, b) => $"{a}{separator}{b}").Trim(separator.ToCharArray());
}
