namespace Talepreter.Exceptions;

[GenerateSerializer]
public class CommandExecutionException : CommandException
{
    public CommandExecutionException() { }
    public CommandExecutionException(string message) : base(message) { }
    public CommandExecutionException(string message, Exception ex) : base(message, ex) { }

    public CommandExecutionException(string commandData, string message) : base(commandData, message) { }
    public CommandExecutionException(string commandData, string message, Exception ex) : base(commandData, message, ex) { }
}
