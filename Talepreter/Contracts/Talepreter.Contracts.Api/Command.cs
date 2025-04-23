namespace Talepreter.Contracts.Api;

public class Command
{
    public int Phase { get; init; }
    public int Index { get; init; }

    public string Tag { get; init; } = default!;
    public string Target { get; init; } = default!;
    public string? Parent { get; init; }
    public NamedParameter[] NamedParameters { get; init; } = default!;
    public string[] ArrayParameters { get; init; } = default!;
    public string? Comment { get; init; } = default!;

    public override string ToString() => $"CMD[{Index}]: {Tag} {Target}";
}
