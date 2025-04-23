using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Model.Document.Extensions.ActorSvc;
using Talepreter.Operations.Grains;

namespace Talepreter.ActorSvc.Grains;

[GenerateSerializer, StatelessWorker]
public class ActorContainerGrain : ContainerGrain<ActorContainerGrain>, IActorContainerGrain
{
    public ActorContainerGrain(ILogger<ActorContainerGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task InitializePublishExtension(Guid taleId, Guid taleVersionId, IDocumentDbContext dbContext, CancellationToken token)
    {
        // do we need more stuff?
        var extension = new WorldExtension() {
            DocumentId = WorldExtension.IdConst,
            LastUpdate = DateTime.UtcNow,
            LastUpdatedChapter = -1,
            LastUpdatedPageInChapter = -1
        };
        await dbContext.OverwriteAsync(taleId, taleVersionId, extension, token);
        token.ThrowIfCancellationRequested();
    }
}
