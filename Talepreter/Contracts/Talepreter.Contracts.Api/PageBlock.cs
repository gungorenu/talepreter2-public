namespace Talepreter.Contracts.Api;

public class PageBlock
{
    public long Date { get; init; }
    public long Stay { get; init; } = 0;
    public Location Location { get; init; } = default!;
    public Location? Travel { get; init; }
    public long? Voyage { get; init; }
}

public class Location
{
    public string Settlement { get; init; } = default!;
    public string? Extension { get; init; }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Extension)) return Settlement;
        else return $"{Settlement},{Extension}";
    }
}