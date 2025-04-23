namespace Talepreter.Operations.Workload;

public abstract class WorkTaskArgument
{
    public Guid TaleId { get; init; } = default!;
    public Guid TaleVersionId { get; init; } = default!;
}
