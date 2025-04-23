using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations;
using Talepreter.Operations.Grains;

namespace Talepreter.WorldSvc.Grains;

[GenerateSerializer]
public class CacheGrain : CommandGrain, ICacheGrain
{
    public CacheGrain(ILogger<CacheGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        if (commandInfo.Command.Tag != Model.Command.CommandIds.Cache)
            throw new CommandExecutionException(commandInfo.Command.ToString()!, "Command is not recognized for execution");

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<ICacheGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(ICacheGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}
