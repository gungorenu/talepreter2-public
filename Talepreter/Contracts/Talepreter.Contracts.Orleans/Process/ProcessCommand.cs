namespace Talepreter.Contracts.Orleans.Process;

[GenerateSerializer]
public class ProcessCommand : RawCommand
{
    [Id(0)] public int Phase { get; init; }
    [Id(1)] public int Index { get; init; }
}
