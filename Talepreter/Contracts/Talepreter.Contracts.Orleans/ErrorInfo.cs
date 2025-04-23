namespace Talepreter.Contracts.Orleans;

[GenerateSerializer]
public class ErrorInfo
{
    [Id(0)] public string Message { get; init; } = default!;
    [Id(1)] public string Type { get; init; } = default!;
    [Id(2)] public string? Stacktrace { get; init; }
}
