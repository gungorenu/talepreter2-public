namespace Talepreter.Contracts.Messaging;

public class CancelPageOperationRequest
{
    public Guid TaleId { get; init; } = default!;
    public Guid TaleVersionId { get; init; } = default!;
}
