namespace Talepreter.Operations;

public class PageTimelineInfo
{
    public record Location(string Settlement, string? Extension);

    public long Start { get; init; }
    public Location StartLocation { get; init; } = default!;
    public Location? TravelTo { get; init; }
    public long? Voyage { get; init; }
    public long Stay { get; init; }

    public Location EndLocation => TravelTo ?? StartLocation;
    public long Today => Start + Stay + (Voyage ?? 0);
    public long LastAtLocation => Start + Stay;
}
