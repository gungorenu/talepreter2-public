namespace Talepreter.Contracts.Messaging;

public class ProcessOperationResponse
{
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }
    public DateTime OperationTime { get; init; }
    public int Chapter { get; init; }
    public int Page { get; init; }
    public ProcessStatus Status { get; init; } = ProcessStatus.None;
}
