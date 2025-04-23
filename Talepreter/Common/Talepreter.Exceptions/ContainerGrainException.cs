namespace Talepreter.Exceptions;

[GenerateSerializer]
public class ContainerGrainException : Exception
{
    public ContainerGrainException() { }
    public ContainerGrainException(string message) : base(message) { }
    public ContainerGrainException(string message, Exception ex) : base(message, ex) { }
}
