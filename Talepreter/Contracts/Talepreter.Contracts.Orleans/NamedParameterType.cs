namespace Talepreter.Contracts.Orleans;

[GenerateSerializer]
public enum NamedParameterType
{
    [Id(0)] Set = 0, // no sign means set value, "param:"
    [Id(1)] Add = 1, // + sign means add value, "+param:"
    [Id(2)] Remove = 2, // - sign means subtract value, "-param:"
    [Id(3)] Reset = 3 // . sign means reset value, ".param:" 
}
