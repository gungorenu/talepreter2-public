namespace Talepreter.Contracts.Messaging;

public class NamedParameter
{
    public NamedParameterType Type { get; init; } = NamedParameterType.Set;
    public string Name { get; init; } = default!;
    public string Value { get; init; } = "";
}
