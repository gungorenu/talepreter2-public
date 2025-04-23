namespace Talepreter.Common;

/// <summary>
/// all of them are in seconds unless mentioned
/// </summary>
public static class Timeouts
{
    public static int GrainOperationTimeout { get; private set; } = 15;
    public static int TaskManagerTimeout { get; private set; } = 30;
    public static int TaskManagerDelayTimeout { get; private set; } = 50; // in ms
    public static int RabbitMQExecuteTimeout { get; private set; } = 30;
    public static int OperationTimeout { get; private set; } = 15; // used in GUI
    public static int WorktaskTimeout { get; private set; } = 15;

    static Timeouts()
    {
        GrainOperationTimeout = GetTimeout("GrainOperationTimeout", GrainOperationTimeout);
        TaskManagerTimeout = GetTimeout("TaskManagerTimeout", TaskManagerTimeout);
        TaskManagerDelayTimeout = GetTimeout("TaskManagerDelayTimeout", TaskManagerDelayTimeout);
        RabbitMQExecuteTimeout = GetTimeout("RabbitMQExecuteTimeout", RabbitMQExecuteTimeout);
        OperationTimeout = GetTimeout("OperationTimeout", OperationTimeout);
        WorktaskTimeout = GetTimeout("WorktaskTimeout", WorktaskTimeout);
    }

    private static int GetTimeout(string varName, int @default)
    {
        var vr = EnvironmentVariableHandler.TryReadEnvVar(varName);
        if (!string.IsNullOrEmpty(vr)) return vr.ToInt();
        return @default;
    }

}
