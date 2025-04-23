using Talepreter.Contracts.Orleans.Execute;

namespace Talepreter.Contracts.Orleans.Grains;

/// <summary>
/// our basic command processing grain, base type
/// </summary>
public interface ICommandGrain : IGrainWithStringKey
{
    Task<ExecuteCommandResponse> ExecuteCommand(ExecuteCommandContext commandInfo);
}
