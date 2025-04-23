namespace Talepreter.Contracts.Orleans.Execute;

[GenerateSerializer]
public class ExecuteCommandResponse
{
    [Id(0)] public ExecuteResult Status { get; init; }
    [Id(1)] public ErrorInfo? Error { get; init; } = default!;
    [Id(2)] public RawCommand? Command { get; init; } = default!; // only set when error occurs
}
