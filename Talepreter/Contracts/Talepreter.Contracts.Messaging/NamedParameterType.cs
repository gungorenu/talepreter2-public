namespace Talepreter.Contracts.Messaging;

public enum NamedParameterType
{
    Set = 0, // no sign means set value, "param:"
    Add = 1, // + sign means add value, "+param:"
    Remove = 2, // - sign means subtract value, "-param:"
    Reset = 3 // . sign means reset value, ".param:" 
}
