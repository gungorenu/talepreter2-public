using MongoDB.Driver;
using Talepreter.Model.Document;

namespace Talepreter.Data.DocumentDbContext;

public interface IDocumentDbContext
{
    string CollectionName(Guid taleId, Guid taleVersionId);

    Task InitializeTaleVersionAsync(Guid taleId, Guid taleVersionId, CancellationToken token);
    Task BackupTaleVersionAsync(Guid taleId, Guid sourceTaleVersionId, Guid targetTaleVersionId, CancellationToken token);
    Task PurgeTaleAsync(Guid taleId, CancellationToken token);
    Task PurgeTaleVersionAsync(Guid taleId, Guid taleVersionId, CancellationToken token);

    Task UpsertAsync<T>(Guid taleId, Guid taleVersionId, T document, UpdateDefinition<T> documentUpdate, DocumentState state, CancellationToken token) where T : DocumentBase;
    Task<T?> GetAsync<T>(Guid taleId, Guid taleVersionId, string dbId, DocumentState state, CancellationToken token) where T : DocumentBase;
    Task<T[]?> GetManyAsync<T>(Guid taleId, Guid taleVersionId, FilterDefinition<T> filter, DocumentState state, CancellationToken token) where T : DocumentBase;
    Task OverwriteAsync<T>(Guid taleId, Guid taleVersionId, T document, CancellationToken token) where T : DocumentBase;
    Task<long> CountAsync<T>(Guid taleId, Guid taleVersionId, FilterDefinition<T> filter, int limit, CancellationToken token) where T : DocumentBase;
}
