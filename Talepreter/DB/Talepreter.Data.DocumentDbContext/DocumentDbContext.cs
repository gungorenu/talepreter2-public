using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Talepreter.Common;
using Talepreter.Model.Document;

namespace Talepreter.Data.DocumentDbContext;

public class DocumentDbContext : IDocumentDbContext
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;

    static DocumentDbContext()
    {
        ConventionRegistry.Register(nameof(ImmutableTypeClassMapConvention), new ConventionPack { new ImmutableTypeClassMapConvention() }, type => true);
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public DocumentDbContext()
    {
        var connectionString = EnvironmentVariableHandler.ReadEnvVar("MongoDBConnection");
        var dbName = EnvironmentVariableHandler.ReadEnvVar("MongoDBName");

        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(dbName);
    }

    public string CollectionName(Guid taleId, Guid taleVersionId) => $"T{taleId:N}.P{taleVersionId:N}";

    public async Task InitializeTaleVersionAsync(Guid taleId, Guid taleVersionId, CancellationToken token)
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        await _database.DropCollectionAsync(collectionName, token);
        await CreateNewPublishAsync(collectionName, token);
    }

    public async Task BackupTaleVersionAsync(Guid taleId, Guid sourceTaleVersionId, Guid targetTaleVersionId, CancellationToken token)
    {
        var sourceCollection = CollectionName(taleId, sourceTaleVersionId);
        var targetCollection = CollectionName(taleId, targetTaleVersionId);

        var sourceColl = _database.GetCollection<BsonDocument>(sourceCollection);
        var pipeline = new[] { new BsonDocument { { "$out", targetCollection } } };
        await sourceColl.AggregateAsync<BsonDocument>(pipeline, cancellationToken: token);
        await CreateIndexes(targetCollection, token);
    }

    public async Task PurgeTaleAsync(Guid taleId, CancellationToken token)
    {
        var result = await _database.ListCollectionNamesAsync(
            new ListCollectionNamesOptions { Filter = Builders<BsonDocument>.Filter.Regex("name", $"{taleId:N}.*") }, token);
        var list = await result.ToListAsync(token);
        foreach (var collection in list)
        {
            if (string.IsNullOrEmpty(collection)) continue;
            await _database.DropCollectionAsync(collection, token);
        }
    }

    public async Task PurgeTaleVersionAsync(Guid taleId, Guid taleVersionId, CancellationToken token)
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        await _database.DropCollectionAsync(collectionName, token);
    }

    public async Task UpsertAsync<T>(Guid taleId, Guid taleVersionId, T document, UpdateDefinition<T> documentUpdate, DocumentState state, CancellationToken token) where T : DocumentBase
    {
        ArgumentNullException.ThrowIfNull(documentUpdate, nameof(documentUpdate));
        var collectionName = CollectionName(taleId, taleVersionId);
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq(t => t.Id, document.Id);
        if (state != DocumentState.Any) filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(t => t.State, state),
            Builders<T>.Filter.Eq(t => t.Id, document.Id));

        var result = await collection.UpdateOneAsync(filter, documentUpdate, new UpdateOptions { IsUpsert = true }, token);
        if (result?.MatchedCount > 1) throw new InvalidOperationException($"Expected to update one document but instead got {result?.MatchedCount}");
    }

    public async Task<T?> GetAsync<T>(Guid taleId, Guid taleVersionId, string dbId, DocumentState state, CancellationToken token) where T : DocumentBase
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq(t => t.Id, dbId);
        if (state != DocumentState.Any) filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(t => t.State, state),
            Builders<T>.Filter.Eq(t => t.Id, dbId));
        var results = await collection.FindAsync<T>(filter, default!, token);
        var found = await results.ToListAsync(token);
        if (found.Count > 1) throw new InvalidOperationException($"Expected to find one document but instead got {found.Count}");
        return found.FirstOrDefault();
    }

    public async Task<T[]?> GetManyAsync<T>(Guid taleId, Guid taleVersionId, FilterDefinition<T> filter, DocumentState state, CancellationToken token) where T : DocumentBase
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        var collection = _database.GetCollection<T>(collectionName);
        var customFilter = filter;
        if (state != DocumentState.Any) customFilter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(t => t.State, state), filter);
        var results = await collection.FindAsync<T>(customFilter, default!, token);
        var found = await results.ToListAsync(token);
        return [.. found];
    }

    public async Task OverwriteAsync<T>(Guid taleId, Guid taleVersionId, T document, CancellationToken token) where T : DocumentBase
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        var collection = _database.GetCollection<T>(collectionName);
        var filter = Builders<T>.Filter.Eq(t => t.Id, document.Id);
        var result = await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, token)
            ?? throw new InvalidOperationException($"Overwrite operation got null result");
        if (result.MatchedCount > 1) throw new InvalidOperationException($"Expected to update one document but instead got {result?.MatchedCount}");
        if (!result.IsAcknowledged) throw new InvalidOperationException($"Overwrite operation is not acknowledged");
    }

    public async Task<long> CountAsync<T>(Guid taleId, Guid taleVersionId, FilterDefinition<T> filter, int limit, CancellationToken token) where T : DocumentBase
    {
        var collectionName = CollectionName(taleId, taleVersionId);
        var collection = _database.GetCollection<T>(collectionName);
        return await collection.CountDocumentsAsync(filter, new CountOptions { Limit = limit }, token);
    }

    // --

    private async Task CreateNewPublishAsync(string collectionName, CancellationToken token)
    {
        await _database.CreateCollectionAsync(collectionName, cancellationToken: token);
        await CreateIndexes(collectionName, token);
    }

    private async Task CreateIndexes(string collectionName, CancellationToken token)
    {
        var collection = _database.GetCollection<DocumentBase>(collectionName);
        var builder = Builders<DocumentBase>.IndexKeys;

        var def = new CreateIndexModel<DocumentBase>(builder.Ascending(a => a.Id), new CreateIndexOptions { Name = "tp_idx_id" });
        await collection.Indexes.CreateOneAsync(def, cancellationToken: token);

        token.ThrowIfCancellationRequested();

        def = new CreateIndexModel<DocumentBase>(builder.Combine(builder.Ascending(a => a.Id), builder.Ascending(a => a.DocumentType)),
            new CreateIndexOptions { Name = "tp_idx_id_doctype" });
        await collection.Indexes.CreateOneAsync(def, cancellationToken: token);

        token.ThrowIfCancellationRequested();

        def = new CreateIndexModel<DocumentBase>(builder.Combine(builder.Ascending(a => a.Id), builder.Ascending(a => a.DocumentType), builder.Ascending(a => a.State)),
            new CreateIndexOptions { Name = "tp_idx_id_doctype_state" });
        await collection.Indexes.CreateOneAsync(def, cancellationToken: token);

        token.ThrowIfCancellationRequested();

        def = new CreateIndexModel<DocumentBase>(builder.Ascending(a => a.ParentId), new CreateIndexOptions { Name = "tp_idx_id_doctype_parent" });
        await collection.Indexes.CreateOneAsync(def, cancellationToken: token);

        token.ThrowIfCancellationRequested();

        var builderS = Builders<DocumentBase<string>>.IndexKeys;
        var collectionS = _database.GetCollection<DocumentBase<string>>(collectionName);
        var defS = new CreateIndexModel<DocumentBase<string>>(builderS.Combine(builderS.Ascending(a => a.Id), builderS.Ascending(a => a.DocumentType), builderS.Ascending(a => a.DocumentId)),
            new CreateIndexOptions { Name = "tp_idx_id_doctype_docid" });
        await collectionS.Indexes.CreateOneAsync(defS, cancellationToken: token);

        token.ThrowIfCancellationRequested();
    }
}
