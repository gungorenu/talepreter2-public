using Talepreter.Contracts.Orleans.Execute;

namespace Talepreter.Contracts.Orleans.Grains;

/// <summary>
/// an expansion of command grain with capability of executing triggers
/// </summary>
public interface ITriggeredCommandGrain : ICommandGrain
{
    Task<ExecuteCommandResponse> ExecuteTrigger(ExecuteTriggerContext triggerInfo);
}
