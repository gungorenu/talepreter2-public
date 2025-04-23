namespace Talepreter.Contracts.Messaging;

public class ProcessCommandResponse
{
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }
    public DateTime OperationTime { get; init; }
    public int Chapter { get; init; }
    public int Page { get; init; }
    public ResponsibleService Service { get; init; } = ResponsibleService.None;
    public ProcessStatus Status { get; init; } = ProcessStatus.None;
    public ErrorInfo? Error { get; init; }
    public string CommandData { get; init; } = default!;
}
