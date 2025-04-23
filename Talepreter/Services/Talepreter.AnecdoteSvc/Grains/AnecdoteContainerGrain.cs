using Orleans.Concurrency;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Model.Document.Extensions.AnecdoteSvc;
using Talepreter.Operations.Grains;

namespace Talepreter.AnecdoteSvc.Grains;

[GenerateSerializer, StatelessWorker]
public class AnecdoteContainerGrain : ContainerGrain<AnecdoteContainerGrain>, IAnecdoteContainerGrain
{
    public AnecdoteContainerGrain(ILogger<AnecdoteContainerGrain> logger, IDocumentDbContext documentDbContext) : base(logger, documentDbContext) { }

    protected override async Task InitializePublishExtension(Guid taleId, Guid taleVersionId, IDocumentDbContext dbContext, CancellationToken token)
    {
        var extension = new WorldExtension()
        {
            DocumentId = WorldExtension.IdConst,
            LastUpdate = DateTime.UtcNow,
            LastUpdatedChapter = -1,
            LastUpdatedPageInChapter = -1
        };
        await dbContext.OverwriteAsync(taleId, taleVersionId, extension, token);
        token.ThrowIfCancellationRequested();
    }
}
