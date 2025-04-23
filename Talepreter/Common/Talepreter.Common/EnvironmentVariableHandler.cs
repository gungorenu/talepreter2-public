namespace Talepreter.Common;

public static class EnvironmentVariableHandler
{
    public static string ReadEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value)) 
            throw new EnvironmentVariableException(name);

        return value;
    }

    public static string? TryReadEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return value;
    }
}
