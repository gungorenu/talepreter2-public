namespace Talepreter.Contracts.Orleans.Process;

[GenerateSerializer]
public enum TaleState
{
    [Id(0)] Idle = 0, // idle state, will move to this after operation is done
    [Id(1)] Writing = 1, // by-pass high speed data upload state
    [Id(2)] Processing = 2, // blocked data upload but now processes stuff at other services
    [Id(3)] Publishing = 3, // blocked data upload and processing but now publishes last good state or specific state

    [Id(4)] Busy = -1 // intermediate state, doing something which grain might not have full control over
}
