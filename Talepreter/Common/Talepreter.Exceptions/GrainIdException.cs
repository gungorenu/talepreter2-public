namespace Talepreter.Exceptions;

[GenerateSerializer]
public class GrainIdException : Exception
{
    public GrainIdException() { }
    public GrainIdException(string message) : base(message) { }
    public GrainIdException(string message, Exception innerException) : base(message, innerException) { }
}
