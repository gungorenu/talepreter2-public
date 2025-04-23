namespace Talepreter.Exceptions;

[GenerateSerializer]
public class MissingMapperException : Exception
{
    public MissingMapperException() { }
    public MissingMapperException(string message) : base(message) { }
    public MissingMapperException(string message, Exception innerException) : base(message, innerException) { }

    public static MissingMapperException Fault<S, D>(S value) => new($"Mapping from {typeof(S).FullName} to {typeof(D).FullName} failed for object value {value}");
}
