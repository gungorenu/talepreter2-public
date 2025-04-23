namespace Talepreter.Contracts.Orleans;

[GenerateSerializer]
public class RawTrigger
{
    [Id(0)] public string Target { get; init; } = default!;
    [Id(1)] public string Id { get; init; } = default!;
    [Id(2)] public string Type { get; init; } = default!;
    [Id(3)] public string Parameter { get; init; } = default!;
    [Id(4)] public string Grain { get; init; } = default!;
    [Id(5)] public long TriggerAt { get; init; } = default!;

    public override string ToString()
    {
        return $"`TRIGGER`: {Target} `>>` {Grain} `:` <u>type:</u> {Type}, <u>id:</u> {Id}, <u>parameter:</u> {Parameter}, <u>at:</u> {TriggerAt}";
    }

    public RawCommand ToCommand()
    {
        return new RawCommand
        {
            Tag = "TRIGGER",
            Target = Target,
            Parent = null,
            Comment = null,
            ArrayParameters = [],
            NamedParameters = [new NamedParameter{ Name = "id", Type = NamedParameterType.Set, Value = Id },
                    new NamedParameter{ Name = "type", Type = NamedParameterType.Set, Value = Type },
                    new NamedParameter{ Name = "parameter", Type = NamedParameterType.Set, Value = Parameter },
                    new NamedParameter{ Name = "grain", Type = NamedParameterType.Set, Value = Grain },
                    new NamedParameter{ Name = "at", Type = NamedParameterType.Set, Value = TriggerAt.ToString() }]
        };
    }
}
