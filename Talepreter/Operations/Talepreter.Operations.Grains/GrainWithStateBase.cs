using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Talepreter.Operations.Grains;

[GenerateSerializer]
public abstract class GrainWithStateBase<TState> : GrainBase
    where TState : class
{
    private readonly IPersistentState<TState> _state;

    protected GrainWithStateBase(IPersistentState<TState> persistentState, ILogger logger)
        : base(logger)
    {
        _state = persistentState;
    }

    protected TState State => _state.State;

    protected async Task SaveStateAsync(Func<TState, Task> action)
    {
        await action(_state.State);
        await _state.WriteStateAsync();
    }
}
