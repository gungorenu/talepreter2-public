namespace Talepreter.Exceptions;

[GenerateSerializer]
public class ConsumerJobException : Exception
{
    public ConsumerJobException() { }
    public ConsumerJobException(string message) : base(message) { }
    public ConsumerJobException(string message, Exception ex) : base(message, ex) { }
}
