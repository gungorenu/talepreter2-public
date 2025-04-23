namespace Talepreter.Exceptions;

[GenerateSerializer]
public class TalePublishException : Exception
{
    public TalePublishException() { }
    public TalePublishException(string message) : base(message) { }
    public TalePublishException(string message, Exception innerException) : base(message, innerException) { }
}
