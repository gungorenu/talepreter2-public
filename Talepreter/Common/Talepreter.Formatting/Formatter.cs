using System.Text.RegularExpressions;
using Talepreter.Exceptions;

namespace Talepreter.Formatting;

public static class Formatter
{
    public static readonly IFormatStyle C1 = new InternalFormat(new Regex(@"(?<=\`)[^`]+(?=\`)"), "`{0}`", FormatStyle.C1);
    public static readonly IFormatStyle C2 = new InternalFormat(new Regex(@"(?<=\*)[^*]+(?=\*)"), "*{0}*", FormatStyle.C2);
    public static readonly IFormatStyle C3 = new InternalFormat(new Regex(@"(?<=\*\*)[^*]+(?=\*\*)"), "**{0}**", FormatStyle.C3);
    public static readonly IFormatStyle C4 = new InternalFormat(new Regex(@"(?<=\=\=)[^\=]+(?=\=\=)"), "=={0}==", FormatStyle.C4);
    public static readonly IFormatStyle C5 = new InternalFormat(new Regex(@"(?<=\~\~)[^\~]+(?=\~\~)"), "~~{0}~~", FormatStyle.C5);
    public static readonly IFormatStyle C6 = new NewInternalFormat(new Regex(@"(?<=_)[^_]+(?=_)"), new Regex(@"(?<=\<u\>)[^\<^\>]+(?=\<\/u\>)"), "_{0}_", "<u>{0}</u>", FormatStyle.C6);
    public static readonly IFormatStyle C7 = new InternalFormat(new Regex(@"(?<=\^)[^\^]+(?=\^)"), "^{0}^", FormatStyle.C7);
    public static readonly IFormatStyle C8 = new InternalFormat(new Regex(@"(?<=\~)[^\~]+(?=\~)"), "~{0}~", FormatStyle.C8);
    public static readonly IFormatStyle C9 = new InternalFormat(new Regex(@"((?<=\|)[^\|]+(?=\|))"), "|{0}|", FormatStyle.C9);
    public static readonly IFormatStyle C0 = new InternalFormat(new Regex(@".*"), "{0}", FormatStyle.Unset);

    public static string? FormatWithCondition(bool condition, IFormatStyle trueStyle, IFormatStyle falseStyle, string? value, bool newFormat = true)
    {
        return (condition ? trueStyle : falseStyle).Format(value, newFormat);
    }
    public static string? FormatWithCondition(bool condition, IFormatStyle trueStyle, IFormatStyle falseStyle, int? value, bool newFormat = true)
    {
        return (condition ? trueStyle : falseStyle).Format(value, newFormat);
    }
    public static IFormatStyle Style(this FormatStyle style)
    {
        return style switch
        {
            FormatStyle.C1 => C1,
            FormatStyle.C2 => C2,
            FormatStyle.C3 => C3,
            FormatStyle.C4 => C4,
            FormatStyle.C5 => C5,
            FormatStyle.C6 => C6,
            FormatStyle.C7 => C7,
            FormatStyle.C8 => C8,
            FormatStyle.C9 => C9,
            FormatStyle.Unset => C0,
            _ => throw MissingMapperException.Fault<FormatStyle,IFormatStyle>(style)
        };
    }
    public static string? TrimFormatting(this string? self)
    {
        if (self == null) return null;
        var t = self;
        // because repeated tags can be found, trimming has to do first C3 C4 C5 and then others
        if (C3.IsInFormat(t)) t = C3.Trim(t);
        if (C4.IsInFormat(t)) t = C4.Trim(t);
        if (C5.IsInFormat(t)) t = C5.Trim(t);

        if (C1.IsInFormat(t)) t = C1.Trim(t);
        if (C2.IsInFormat(t)) t = C2.Trim(t);
        if (C6.IsInFormat(t)) t = C6.Trim(t);
        if (C7.IsInFormat(t)) t = C7.Trim(t);
        if (C8.IsInFormat(t)) t = C8.Trim(t);
        if (C9.IsInFormat(t)) t = C9.Trim(t);
        return t;
    }
    public static FormatStyle GetFormat(this string self)
    {
        // because repeated tags can be found, trimming has to do first C3 C4 C5 and then others
        if (C3.IsInFormat(self)) return FormatStyle.C3;
        else if (C4.IsInFormat(self)) return FormatStyle.C4;
        else if (C5.IsInFormat(self)) return FormatStyle.C5;

        else if (C1.IsInFormat(self)) return FormatStyle.C1;
        else if (C2.IsInFormat(self)) return FormatStyle.C2;
        else if (C6.IsInFormat(self)) return FormatStyle.C6;
        else if (C7.IsInFormat(self)) return FormatStyle.C7;
        else if (C8.IsInFormat(self)) return FormatStyle.C8;
        else if (C9.IsInFormat(self)) return FormatStyle.C9;
        return FormatStyle.Unset;
    }

    public static string FormatModifier(this int value, bool negativeBad = true)
    {
        if (value == 0) return C3.Format("0")!;
        if (value > 0 ^ negativeBad) return C8.Format($"{value:+0;-#}")!;
        return C7.Format($"{value:+0;-#}")!;
    }

    public static string FormatModifier(this long value, bool negativeBad = true)
    {
        if (value == 0) return C3.Format("0")!;
        if (value > 0 ^ negativeBad) return C8.Format($"{value:+0;-#}")!;
        return C7.Format($"{value:+0;-#}")!;
    }

    private class InternalFormat : IFormatStyle
    {
        private readonly Regex _regex;
        private readonly string _pattern;
        private readonly FormatStyle _style;

        public InternalFormat(Regex regex, string pattern, FormatStyle style)
        {
            _regex = regex;
            _pattern = pattern;
            _style = style;
        }
        FormatStyle IFormatStyle.Style => _style;
        bool IFormatStyle.IsInFormat(string? value)
        {
            if (value == null) return false;
            return _regex.IsMatch(value);
        }
        string? IFormatStyle.Trim(string? value)
        {
            if (value == null) return null;
            if (!_regex.IsMatch(value)) return value;
            return _regex.Match(value).Value.Trim();
        }
        string? IFormatStyle.Format(string? value, bool newFormat)
        {
            if (value == null) return null;
            return string.Format(_pattern, value);
        }
        string? IFormatStyle.Format(int? value, bool newFormat)
        {
            if (value == null) return null;
            return string.Format(_pattern, value);
        }
        string? IFormatStyle.Clean(string? value)
        {
            if (value == null) return null;
            string t = value!;
            while (_regex.IsMatch(t))
            {
                var k = _regex.Match(t).Value;
                t = t.Replace(string.Format(_pattern, k), k);
            }
            return t;
        }
        string? IFormatStyle.GetMatchOnce(string? value, bool throwExc)
        {
            if (value != null)
            {
                var matches = _regex.Matches(value);
                if (matches.Count == 0) return null;
                if (matches.Count == 1) return matches[0].Value.Trim();
            }
            if (throwExc) throw new InvalidOperationException($"Style {_style} matches more than once or none but expected only once");
            return null;
        }
        string IFormatStyle.ExpectMatchOnce(string value, bool throwExc)
        {
            var matches = _regex.Matches(value);
            if (matches.Count == 1) return matches[0].Value.Trim();
            if (throwExc) throw new InvalidOperationException($"Style {_style} matches more than once or none but expected only once");
            return default!;
        }
    }

    private class NewInternalFormat : IFormatStyle
    {
        private readonly string _newPattern;
        private readonly string _oldPattern;
        private readonly Regex _newRegex;
        private readonly Regex _oldRegex;
        private readonly FormatStyle _style;

        public NewInternalFormat(Regex newRegex, Regex oldRegex, string newPattern, string oldPattern, FormatStyle style)
        {
            _newRegex = newRegex;
            _oldRegex = oldRegex;
            _newPattern = newPattern;
            _oldPattern = oldPattern;
            _style = style;
        }
        FormatStyle IFormatStyle.Style => _style;
        bool IFormatStyle.IsInFormat(string? value)
        {
            if (value == null) return false;
            return _newRegex.IsMatch(value) || _oldRegex.IsMatch(value);
        }
        string? IFormatStyle.Trim(string? value)
        {
            if (value == null) return null;
            if (_newRegex.IsMatch(value)) return _newRegex.Match(value).Value.Trim();
            if (_oldRegex.IsMatch(value)) return _oldRegex.Match(value).Value.Trim();
            return value;
        }
        string? IFormatStyle.Format(string? value, bool newFormat)
        {
            if (value == null) return null;
            return string.Format(newFormat ? _newPattern : _oldPattern, value);
        }
        string? IFormatStyle.Format(int? value, bool newFormat)
        {
            if (value == null) return null;
            return string.Format(newFormat ? _newPattern : _oldPattern, value);
        }
        string? IFormatStyle.Clean(string? value)
        {
            if (value == null) return null;
            if (!_newRegex.IsMatch(value) && !_oldRegex.IsMatch(value))
                return value;
            string t = value!;
            while (_newRegex.IsMatch(t))
            {
                var k = _newRegex.Match(t).Value;
                t = t.Replace(string.Format(_newPattern, k), k);
            }
            while (_oldRegex.IsMatch(t))
            {
                var k = _oldRegex.Match(t).Value;
                t = t.Replace(string.Format(_oldPattern, k), k);
            }
            return t;
        }
        string? IFormatStyle.GetMatchOnce(string? value, bool throwExc)
        {
            if (value != null)
            {
                var matches1 = _oldRegex.Matches(value);
                var matches2 = _newRegex.Matches(value);
                if (matches1.Count == 0 && matches2.Count == 0) return null;
                if (matches1.Count == 1 && matches2.Count == 0) return matches1[0].Value.Trim();
                if (matches1.Count == 0 && matches2.Count == 1) return matches2[0].Value.Trim();
            }
            if (throwExc) throw new InvalidOperationException($"Style {_style} matches more than once but expected only once");
            return null;
        }
        string IFormatStyle.ExpectMatchOnce(string value, bool throwExc)
        {
            var matches1 = _oldRegex.Matches(value);
            var matches2 = _newRegex.Matches(value);
            if (matches1.Count == 0 && matches2.Count == 0) throw new InvalidOperationException($"Style {_style} matches none but expected once");
            if (matches1.Count == 1 && matches2.Count == 0) return matches1[0].Value.Trim();
            if (matches1.Count == 0 && matches2.Count == 1) return matches2[0].Value.Trim();
            if (throwExc) throw new InvalidOperationException($"Style {_style} matches more than once or none but expected only once");
            return default!;
        }
    }
}
