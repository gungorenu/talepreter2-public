using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.BaseTypes;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.Operations;

namespace Talepreter.ActorSvc.Grains;

[GenerateSerializer]
public class CohortGrain : TriggeredCommandGrain, ICohortGrain
{
    public CohortGrain(ILogger<CohortGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        if (commandInfo.Command.Tag != Model.Command.CommandIds.Cohort)
            throw new CommandExecutionException(commandInfo.Command.ToString()!, "Command is not recognized for execution");

        using var taskDbContext = _scope.ServiceProvider.GetRequiredService<ITaskDbContext>() ?? throw new CommandExecutionException($"{typeof(ITaskDbContext).Name} initialization failed");

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<ICohortGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(ICohortGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, taskDbContext, token);
        await commandExecutor.Execute(commandInfo);
    }

    protected override async Task<TriggerState> ExecuteTriggerAsync(ExecuteTriggerContext context, ITaskDbContext taskDbContext, CancellationToken token)
    {
        if (context.Trigger.Type != Model.Command.CommandIds.TriggerCommand.TriggerList.CohortDeath)
            throw new CommandExecutionException($"Trigger {context.Trigger.Type} cannot execute on Cohort grain");

        var triggerExecutor = _scope.ServiceProvider.GetRequiredService<ITriggerExecutor<ICohortGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(ICohortGrain).Name} trigger executor is invalid");
        triggerExecutor.Initialize(_documentDbContext, taskDbContext, token);
        return await triggerExecutor.Execute(context);
    }
}
