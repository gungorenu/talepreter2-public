using ProcessCommand = Talepreter.Data.BaseTypes.Command;

namespace Talepreter.Operations.Workload;

public class ProcessTaskArgument : WorkTaskArgument
{
    public int Chapter { get; init; } = default!;
    public int Page { get; init; } = default!;
    public string GrainLogId { get; init; } = default!;
    public ProcessCommand[] Commands { get; init; } = default!;
}
