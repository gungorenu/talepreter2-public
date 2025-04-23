namespace Talepreter.Exceptions;

[GenerateSerializer]
public class PublisherJobException : Exception
{
    public PublisherJobException() { }
    public PublisherJobException(string message) : base(message) { }
    public PublisherJobException(string message, Exception ex) : base(message, ex) { }
}
