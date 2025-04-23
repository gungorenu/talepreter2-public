namespace Talepreter.Exceptions;

[GenerateSerializer]
public class CommandProcessingException : CommandException
{
    public CommandProcessingException() { }
    public CommandProcessingException(string message) : base(message) { }
    public CommandProcessingException(string message, Exception ex) : base(message, ex) { }

    public CommandProcessingException(string commandData, string message) : base(commandData, message) { }
    public CommandProcessingException(string commandData, string message, Exception ex) : base(commandData, message, ex) { }
}
