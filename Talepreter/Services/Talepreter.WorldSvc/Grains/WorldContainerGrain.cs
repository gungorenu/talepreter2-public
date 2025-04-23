using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Model.Document;
using Talepreter.Operations.Grains;

namespace Talepreter.WorldSvc.Grains;

[GenerateSerializer, StatelessWorker]
public class WorldContainerGrain : ContainerGrain<WorldContainerGrain>, IWorldContainerGrain
{
    public WorldContainerGrain(ILogger<WorldContainerGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task InitializePublishExtension(Guid taleId, Guid taleVersionId, IDocumentDbContext dbContext, CancellationToken token)
    {
        await dbContext.OverwriteAsync(taleId, taleVersionId, new World(), token);
        token.ThrowIfCancellationRequested();
    }
}
