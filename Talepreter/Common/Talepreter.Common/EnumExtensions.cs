namespace Talepreter.Common;

public static class EnumExtensions
{
    public static E? Parse<E>(this string? self, params E[] exceptions) where E : struct, Enum
    {
        if (self == null) return null;
        var res = Enum.Parse<E>(self.Trim());
        if (exceptions != null && exceptions.Contains(res))
            throw new ArgumentException($"Arg passed to enum {typeof(E).Name} parse has a value parsed into an exceptional value {res}");
        return res;
    }
    public static string? Text<E>(this E? self) where E : struct, Enum
    {
        if (self == null) return null;
        return Enum.GetName(self.Value);
    }
    public static string Text<E>(this E self) where E : struct, Enum
    {
        return Enum.GetName(self)!;
    }
}
