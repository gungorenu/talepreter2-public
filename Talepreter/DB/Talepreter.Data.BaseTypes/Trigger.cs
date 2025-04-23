namespace Talepreter.Data.BaseTypes;

// SQL:EntityFramework Object
public class Trigger
{
    public string Id { get; init; } = default!; // Id is unique and entity based
    public Guid TaleId { get; init; }
    public Guid TaleVersionId { get; init; }
    public DateTime LastUpdate { get; set; }

    // trigger state
    public TriggerState State { get; set; } = TriggerState.Set;
    public long TriggerAt { get; set; } // date of tale, exact same counter with Page's date so must be meaningful and non-zero

    // trigger target
    public string Target { get; init; } = default!; // id of object itself, can be used for foreign key
    public string GrainType { get; init; } = default!; // grain name of the target object
    public string GrainId { get; init; } = default!; // Id of target object
    public string Type { get; init; } = default!; // custom event name, generally this defines what the trigger is about
    public string? Parameter { get; init; } = default!; // additional info to pass, sometimes meaningful
}
