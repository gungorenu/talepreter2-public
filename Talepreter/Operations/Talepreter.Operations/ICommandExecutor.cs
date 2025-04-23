using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.BaseTypes;
using Talepreter.Data.DocumentDbContext;

namespace Talepreter.Operations;

public interface ICommandExecutorBase
{
    void Initialize(IDocumentDbContext dbContext, ITaskDbContext taskDbContext, CancellationToken token);
}

public interface ICommandExecutor<TCommandGrain> : ICommandExecutorBase where TCommandGrain : ICommandGrain
{
    Task Execute(ExecuteCommandContext context);
}

public interface ITriggerExecutor<TTriggerGrain> : ICommandExecutorBase where TTriggerGrain : ITriggeredCommandGrain
{
    Task<TriggerState> Execute(ExecuteTriggerContext context);
}
