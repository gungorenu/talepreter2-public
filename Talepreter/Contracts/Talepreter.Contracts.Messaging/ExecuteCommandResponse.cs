namespace Talepreter.Contracts.Messaging;

public class ExecuteCommandResponse
{
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }
    public DateTime OperationTime { get; init; }
    public int Chapter { get; init; }
    public int Page { get; init; }
    public ResponsibleService Service { get; init; } = ResponsibleService.None;
    public ExecuteStatus Status { get; init; } = ExecuteStatus.None;
    public ErrorInfo? Error { get; init; }
    public string CommandData { get; init; } = default!;
}
