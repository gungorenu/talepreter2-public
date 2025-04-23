using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.BaseTypes;
using Talepreter.Operations.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations;

namespace Talepreter.ActorSvc.Grains;

[GenerateSerializer]
public class ActorGrain : TriggeredCommandGrain, IActorGrain
{
    public ActorGrain(ILogger<ActorGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        using var taskDbContext = _scope.ServiceProvider.GetRequiredService<ITaskDbContext>() ?? throw new CommandExecutionException($"{typeof(ITaskDbContext).Name} initialization failed");

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IActorGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IActorGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, taskDbContext, token);
        await commandExecutor.Execute(commandInfo);
    }

    protected override async Task<TriggerState> ExecuteTriggerAsync(ExecuteTriggerContext context, ITaskDbContext taskDbContext, CancellationToken token)
    {
        var triggerExecutor = _scope.ServiceProvider.GetRequiredService<ITriggerExecutor<IActorGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IActorGrain).Name} trigger executor is invalid");
        triggerExecutor.Initialize(_documentDbContext, taskDbContext, token);
        return await triggerExecutor.Execute(context);
    }
}
