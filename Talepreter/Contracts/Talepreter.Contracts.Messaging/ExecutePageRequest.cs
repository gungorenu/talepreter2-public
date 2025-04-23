namespace Talepreter.Contracts.Messaging;

public class ExecutePageRequest
{
    public Guid TaleId { get; init; } = default!;
    public Guid TaleVersionId { get; init; } = default!;
    public int Chapter { get; init; } = default!;
    public int Page { get; init; } = default!;
    public string GrainLogId { get; init; } = default!;
}
