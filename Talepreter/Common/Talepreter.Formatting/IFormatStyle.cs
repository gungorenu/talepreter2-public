namespace Talepreter.Formatting;

public interface IFormatStyle
{
    FormatStyle Style { get; }
    string? Format(string? value, bool newFormat = true);
    string? Format(int? value, bool newFormat = true);
    string? Trim(string? value);
    string? Clean(string? value);
    bool IsInFormat(string? value);
    string? GetMatchOnce(string? value, bool throwExc = true);
    string ExpectMatchOnce(string value, bool throwExc = true);
}
