using Talepreter.Contracts.Orleans.Grains;

namespace Talepreter.Operations.Workload;

public interface IExecuteTaskGrainFetcher
{
    ICommandGrain FetchCommandGrain(ExecuteTaskArgument arg, IGrainFactory grainFactory, string commandTag, string commandTarget);
    ITriggeredCommandGrain FetchTriggerGrain(ExecuteTaskArgument arg, IGrainFactory grainFactory, string commandTarget, string grainType);
}
