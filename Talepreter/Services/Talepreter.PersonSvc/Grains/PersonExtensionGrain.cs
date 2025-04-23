using Talepreter.Contracts.Orleans.Execute;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Exceptions;
using Talepreter.Operations.Grains;
using Talepreter.Operations;

namespace Talepreter.PersonSvc.Grains;

[GenerateSerializer]
public class PersonExtensionGrain : CommandGrain, IPersonExtensionGrain
{
    public PersonExtensionGrain(ILogger<PersonGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task ExecuteCommandAsync(ExecuteCommandContext commandInfo, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var commandExecutor = _scope.ServiceProvider.GetRequiredService<ICommandExecutor<IPersonExtensionGrain>>() ?? throw new CommandExecutionException($"Registration of {typeof(IPersonExtensionGrain).Name} command executor is invalid");
        commandExecutor.Initialize(_documentDbContext, default!, token);
        await commandExecutor.Execute(commandInfo);
    }
}
