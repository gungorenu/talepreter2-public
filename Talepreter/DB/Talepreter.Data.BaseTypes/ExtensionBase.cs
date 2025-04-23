namespace Talepreter.Data.BaseTypes;

public abstract class ExtensionBase
{
    // last operation time, we are not interested in creation time
    public DateTime OperationTime { get; set; }

    // owner
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }

    public string Id { get; init; } = default!;
    public abstract string Type { get; init; }
}
