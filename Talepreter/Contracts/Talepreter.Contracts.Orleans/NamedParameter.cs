namespace Talepreter.Contracts.Orleans;

[GenerateSerializer]
public class NamedParameter
{
    [Id(0)] public NamedParameterType Type { get; init; } = NamedParameterType.Set;
    [Id(1)] public string Name { get; init; } = default!;
    [Id(2)] public string Value { get; init; } = "";
}
