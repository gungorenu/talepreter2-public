namespace Talepreter.Data.BaseTypes;

public class CommandData
{
    // these two are always a must, rest is optional
    public string Tag { get; init; } = default!;
    public string Target { get; init; } = default!;

    public string? Parent { get; init; }
    public NamedParameter[] NamedParameters { get; init; } = default!;
    public string[] ArrayParameters { get; init; } = default!;
    public string? Comment { get; init; } = default!;

    public override string? ToString()
    {
        var res = $"`{Tag}`: {Target}";
        if (Parent != null) res += $" `>>` {Parent}";
        if (NamedParameters.Length > 0)
        {
            var arr1 = "{";
            var arr2 = "}";
            res += $" `:` ";
            for (int i = 0; i < NamedParameters.Length; i++)
            {
                var par = NamedParameters[i];
                var type = par.Type switch
                {
                    NamedParameterType.Set => "",
                    NamedParameterType.Add => "+",
                    NamedParameterType.Remove => "-",
                    NamedParameterType.Reset => ".",
                    _ => throw new InvalidOperationException("NamedParameter type is unknown"),
                };

                res += $"<u>{type}{par.Name}:</u>";
                if (par.Value.Contains('{')) res += $" {par.Value.Replace("{", arr1).Replace("}", arr2)}";
                else if (par.Value.Contains(',')) res += $" {{ {par.Value} }}";
                else res += $" {par.Value}";

                if (i != NamedParameters.Length - 1) res += ", ";
            }
        }
        if (ArrayParameters.Length > 0)
        {
            res += $" `=` ";
            for (int i = 0; i < ArrayParameters.Length; i++)
            {
                var value = ArrayParameters[i];
                res += value;
                if (i != ArrayParameters.Length - 1) res += ", ";
            }
        }
        if (Comment != null) res += $" `>` {Comment}";
        return res;
    }

}
