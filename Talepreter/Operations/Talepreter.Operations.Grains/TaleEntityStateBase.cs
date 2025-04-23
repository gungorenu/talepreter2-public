using Talepreter.Contracts.Orleans.System;

namespace Talepreter.Operations.Grains
{
    [GenerateSerializer]
    public class TaleEntityStateBase 
    {
        [Id(0)] public Guid TaleId { get; set; } = Guid.Empty;
        [Id(1)] public DateTime LastUpdate { get; set; }
        [Id(2)] public ControllerGrainStatus Status { get; set; } = ControllerGrainStatus.Idle;
    }
}
