using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Exceptions;
using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Operations.Grains;
using Talepreter.Operations;

namespace Talepreter.WorldSvc.Grains;

[GenerateSerializer]
public class WorldGrain : CommandGrain, IWorldGrain
{
    public WorldGrain(ILogger<WorldGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IWorldGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IWorldGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}

