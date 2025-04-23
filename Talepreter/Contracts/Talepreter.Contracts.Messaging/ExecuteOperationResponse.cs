namespace Talepreter.Contracts.Messaging;

public class ExecuteOperationResponse
{
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }
    public DateTime OperationTime { get; init; }
    public int Chapter { get; init; }
    public int Page { get; init; }
    public ExecuteStatus Status { get; init; } = ExecuteStatus.None;
}
