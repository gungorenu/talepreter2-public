namespace Talepreter.Contracts.Messaging;

public class ProcessPageRequest
{
    public Guid TaleId { get; init; } = default!;
    public Guid TaleVersionId { get; init; } = default!;
    public int Chapter { get; init; } = default!;
    public int Page { get; init; } = default!;
    public string GrainLogId { get; init; } = default!;
    public Command[] Commands { get; init; } = default!;
}
