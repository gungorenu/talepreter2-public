using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.Operations;

namespace Talepreter.AnecdoteSvc.Grains;

[GenerateSerializer]
public class AnecdoteExtensionGrain : CommandGrain, IAnecdoteExtensionGrain
{
    public AnecdoteExtensionGrain(ILogger<AnecdoteGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        if (commandInfo.Command.Tag != Model.Command.CommandIds.Page)
            throw new CommandExecutionException(commandInfo.Command.ToString()!, "Command is not recognized for execution");

        token.ThrowIfCancellationRequested();

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IAnecdoteExtensionGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IAnecdoteExtensionGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}
