namespace Talepreter.Contracts.Orleans;

[GenerateSerializer]
public class RawCommand
{
    [Id(0)] public string Tag { get; init; } = default!;
    [Id(1)] public string Target { get; init; } = default!;
    [Id(2)] public string? Parent { get; init; }
    [Id(3)] public NamedParameter[] NamedParameters { get; init; } = default!;
    [Id(4)] public string[] ArrayParameters { get; init; } = default!;
    [Id(5)] public string? Comment { get; init; } = default!;

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
