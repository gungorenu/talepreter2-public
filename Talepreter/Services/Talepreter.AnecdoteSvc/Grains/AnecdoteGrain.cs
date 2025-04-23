using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations;
using Talepreter.Operations.Grains;

namespace Talepreter.AnecdoteSvc.Grains;

[GenerateSerializer]
public class AnecdoteGrain : CommandGrain, IAnecdoteGrain
{
    public AnecdoteGrain(ILogger<AnecdoteGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        if (commandInfo.Command.Tag != Model.Command.CommandIds.Anecdote)
            throw new CommandExecutionException(commandInfo.Command.ToString()!, "Command is not recognized for execution");

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IAnecdoteGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IAnecdoteGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}
