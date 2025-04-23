namespace Talepreter.Operations.Workload;

public class ExecuteTaskArgument : WorkTaskArgument
{
    public int Chapter { get; init; } = default!;
    public int Page { get; init; } = default!;
    public string GrainLogId { get; init; } = default!;
}
