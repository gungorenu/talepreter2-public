namespace Talepreter.Formatting;

public enum FormatStyle
{
    Unset = 0,
    C1 = 1, // `X`   : used for titles/headers/commands
    C2 = 2, // *X*   : used for Name
    C3 = 3, // **X** : used in dialogue, also names
    C4 = 4, // ==X== : used in dialogue
    C5 = 5, // ~~X~~ : used in Person and Settlement names
    C6 = 6, // _X_ and <u>X</u> : used in dialogue, also names. for parsing, <u>X</u> shall be read but transformed to _X_
    C7 = 7, // ^X^   : used in names
    C8 = 8, // ~X~   : used in names
    C9 = 9, // |X|   : for chapters only, like |C#1|
}
