namespace Talepreter.Contracts.Orleans.Process;

[GenerateSerializer, Flags] // it is Flags because of a cumulative result return, it is not a big deal and can be removed too
public enum ProcessResult
{
    [Id(0)] None = 0,
    [Id(1)] Success = 1,
    [Id(2)] Cancelled = 2,
    [Id(3)] Faulted = 4,
    [Id(4)] Timedout = 8,
    [Id(5)] Blocked = 16,
}
