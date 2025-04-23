namespace Talepreter.TaleSvc.Grains.GrainStates;

[GenerateSerializer]
public class TaleGrainState
{
    [Id(0)]
    public DateTime LastUpdate { get; set; }
    [Id(1)]
    public List<Guid> VersionTracker { get; } = [];
}
