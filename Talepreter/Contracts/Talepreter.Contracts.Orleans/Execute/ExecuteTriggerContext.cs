namespace Talepreter.Contracts.Orleans.Execute;

[GenerateSerializer]
public class ExecuteTriggerContext : IExecuteContext
{
    [Id(0)] public Guid TaleId { get; init; }
    [Id(1)] public Guid TaleVersionId { get; init; }
    [Id(2)] public RawTrigger Trigger { get; init; } = default!;
    [Id(3)] public int Chapter { get; init; }
    [Id(4)] public int PageInChapter { get; init; }

    public long? Today() => Trigger.TriggerAt;
}
