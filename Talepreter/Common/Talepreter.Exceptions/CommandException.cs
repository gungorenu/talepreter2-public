namespace Talepreter.Exceptions;

[GenerateSerializer]
public abstract class CommandException : Exception
{
    public CommandException() { }
    public CommandException(string message) : base(message) { }
    public CommandException(string message, Exception ex) : base(message, ex) { }

    public CommandException(string commandData, string message) : base(message) { CommandData = commandData; }
    public CommandException(string commandData, string message, Exception ex) : base(message, ex) { CommandData = commandData; }

    public string CommandData { get => Data["CommandData"]?.ToString() ?? "No command data available"; private set => Data["CommandData"] = value; }
}
