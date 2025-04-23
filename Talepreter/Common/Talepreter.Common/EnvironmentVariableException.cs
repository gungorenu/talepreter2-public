namespace Talepreter.Common;

public class EnvironmentVariableException : Exception
{
    public EnvironmentVariableException(string name)
        : base($"Variable '{name}' is not set in environment")
    {
    }
}
