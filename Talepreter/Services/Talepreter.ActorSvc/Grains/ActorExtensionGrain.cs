using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations;
using Talepreter.Operations.Grains;

namespace Talepreter.ActorSvc.Grains;

[GenerateSerializer]
public class ActorExtensionGrain : CommandGrain, IActorExtensionGrain
{
    public ActorExtensionGrain(ILogger<ActorExtensionGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IActorExtensionGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IActorExtensionGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}
