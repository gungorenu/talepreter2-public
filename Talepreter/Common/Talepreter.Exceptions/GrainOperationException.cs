namespace Talepreter.Exceptions;

[GenerateSerializer]
public class GrainOperationException : Exception
{
    public GrainOperationException() { }
    public GrainOperationException(string message) : base(message) { }
    public GrainOperationException(string message, Exception innerException) : base(message, innerException) { }
    public GrainOperationException(string grainId, string methodName, string message) : base($"[{grainId}].{methodName} {message}") { }
}
