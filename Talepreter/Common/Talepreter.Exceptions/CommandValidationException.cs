namespace Talepreter.Exceptions;

[GenerateSerializer]
public class CommandValidationException : CommandException
{
    public CommandValidationException() { }
    public CommandValidationException(string message) : base(message) { }
    public CommandValidationException(string message, Exception ex) : base(message, ex) { }

    public CommandValidationException(string commandData, string message) : base(commandData, message) { }
    public CommandValidationException(string commandData, string message, Exception ex) : base(commandData, message, ex) { }
}
