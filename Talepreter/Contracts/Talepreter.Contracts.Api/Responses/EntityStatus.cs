namespace Talepreter.Contracts.Api.Responses;

public enum EntityStatus
{
    Idle = 0,
    Processing = 1,
    Processed = 2, // first phase success
    Executing = 3,
    Executed = 4, // second phase success
    Cancelled = 5,
    Faulted = 6,
    Timedout = 7,
    Purged = 8
}
