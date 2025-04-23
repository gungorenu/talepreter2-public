namespace Talepreter.Data.BaseTypes;

public enum TriggerState
{
    Set = 0,
    Triggered = 1,
    Faulted = 2,
    Invalid = 3 // a special case that trigger cannot work because expiration already occured or entity is blocked against trigger now (like became immortal/timeless)
}
