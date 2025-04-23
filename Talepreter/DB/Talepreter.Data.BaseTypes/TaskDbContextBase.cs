using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Talepreter.Data.BaseTypes;

public abstract class TaskDbContextBase : DbContext, ITaskDbContext
{
    private static readonly JsonSerializerOptions SerializationOptions;

    static TaskDbContextBase()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = false,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        SerializationOptions = options;
    }

    public TaskDbContextBase() { }
    public TaskDbContextBase(DbContextOptions contextOptions) : base(contextOptions) { }

    public DbSet<Command> Commands { get; set; }
    public DbSet<Trigger> Triggers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        RegisterCommands(modelBuilder);
        RegisterTriggers(modelBuilder);
        modelBuilder.HasSequence<long>("SubIndexSequence", schema: "shared").IncrementsBy(1);

        base.OnModelCreating(modelBuilder);
    }

    public async Task PurgeEntitiesAsync(Guid taleId, Guid? taleVersionId, CancellationToken token)
    {
        if (taleVersionId == null)
        {
            await Commands.Where(x => x.TaleId == taleId).ExecuteDeleteAsync(token);
            await Triggers.Where(x => x.TaleId == taleId).ExecuteDeleteAsync(token);
        }
        else
        {
            await Commands.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId).ExecuteDeleteAsync(token);
            await Triggers.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId).ExecuteDeleteAsync(token);
        }
    }

    public Task<int> ExecuteAwaitingCommandsMaxPhase(Guid taleId, Guid taleVersionId, int chapter, int page, CancellationToken token)
        => Commands.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.ChapterId == chapter && x.PageId == page).Select(x => x.Phase).DefaultIfEmpty().MaxAsync(token);

    public IQueryable<Command> ExecuteAwaitingCommands(Guid taleId, Guid taleVersionId, int chapter, int page, int phase)
        => Commands.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.ChapterId == chapter && x.PageId == page && x.Phase == phase);

    public IQueryable<Trigger> GetActiveTriggersBefore(Guid taleId, Guid taleVersionId, long date)
        => Triggers.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.TriggerAt < date && x.State == TriggerState.Set);


    public async Task<int> DeleteTriggerAsync(Guid taleId, Guid taleVersionId, string id, CancellationToken token)
    {
        return await Triggers.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.Id == id).ExecuteDeleteAsync(token);
    }

    public async Task<int> ShiftTriggerAsync(Guid taleId, Guid taleVersionId, string id, long newTime, CancellationToken token)
    {
        return await Triggers.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.Id == id && x.TriggerAt < newTime)
            .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.TriggerAt, newTime), token);
    }
    
    public async Task<int> UpdateTriggerAsync(Guid taleId, Guid taleVersionId, string id, TriggerState state, CancellationToken token)
    {
        return await Triggers.Where(x => x.TaleId == taleId && x.TaleVersionId == taleVersionId && x.Id == id)
            .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.State, state), token);
    }

    public async Task<int> BackupToAsync(Guid taleId, Guid taleVersionId, Guid newVersionId, CancellationToken token)
    {
        var parTaleId = new SqlParameter("taleId", taleId);
        var parTaleVersionId = new SqlParameter("sourceVersionId", taleVersionId);
        var parNewVersionId = new SqlParameter("targetVersionId", newVersionId);
        return await Database.ExecuteSqlAsync($"EXEC dbo.TPBACKUPTOVERSION @taleId = {parTaleId}, @sourceVersionId = {parTaleVersionId}, @targetVersionId = {parNewVersionId}", token);
    }

    // --

    protected ModelBuilder RegisterCommands(ModelBuilder builder)
    {
        builder.Entity<Command>().HasKey(p => new { p.TaleId, p.TaleVersionId, p.ChapterId, p.PageId, p.Index, p.Phase, p.SubIndex });
        builder.Entity<Command>().Property(p => p.TaleId).IsRequired(true);
        builder.Entity<Command>().Property(p => p.TaleVersionId).IsRequired(true);
        builder.Entity<Command>().Property(p => p.OperationTime).IsRequired(true);
        builder.Entity<Command>().Property(p => p.ChapterId).IsRequired(true);
        builder.Entity<Command>().Property(p => p.PageId).IsRequired(true);
        builder.Entity<Command>().Property(p => p.Phase).IsRequired(true);
        builder.Entity<Command>().Property(p => p.Index).IsRequired(true);
        builder.Entity<Command>().Property(p => p.SubIndex).HasDefaultValueSql("NEXT VALUE FOR shared.SubIndexSequence");
        builder.Entity<Command>().Property(p => p.Tag).IsRequired(true);
        builder.Entity<Command>().Property(p => p.Target).IsRequired(true);
        builder.Entity<Command>().Property(p => p.Result).IsRequired(true);
        builder.Entity<Command>().HasIndex(p => new { p.TaleId, p.TaleVersionId, p.ChapterId, p.PageId, p.Phase });
        builder.Entity<Command>().Property(a => a.RawData).HasConversion(
            v => JsonSerializer.Serialize(v, SerializationOptions),
            v => JsonSerializer.Deserialize<CommandData>(v, SerializationOptions) ?? new CommandData());

        return builder;
    }

    protected ModelBuilder RegisterTriggers(ModelBuilder builder)
    {
        builder.Entity<Trigger>().HasKey(p => new { p.TaleId, p.TaleVersionId, p.Id });
        builder.Entity<Trigger>().Property(p => p.Id).ValueGeneratedNever().IsRequired(true);
        builder.Entity<Trigger>().Property(p => p.TaleId).IsRequired(true);
        builder.Entity<Trigger>().Property(p => p.TaleVersionId).IsRequired(true);
        builder.Entity<Trigger>().Property(p => p.GrainType).IsRequired(true);
        builder.Entity<Trigger>().Property(p => p.GrainId).IsRequired(true);
        builder.Entity<Trigger>().HasIndex(p => p.TaleId);
        builder.Entity<Trigger>().HasIndex(p => new { p.TaleId, p.TaleVersionId, p.State, p.TriggerAt });
        builder.Entity<Trigger>().HasIndex(p => new { p.TaleId, p.TaleVersionId, p.Id, p.Type });
        builder.Entity<Trigger>().HasIndex(p => new { p.TaleId, p.TaleVersionId, p.Id, p.Type, p.GrainType });
        return builder;
    }

    protected ModelBuilder RegisterExtension<E>(ModelBuilder builder) where E : ExtensionBase
    {
        builder.Entity<E>().HasKey(p => new { p.TaleId, p.TaleVersionId, p.Id });
        builder.Entity<E>().Property(p => p.Id).ValueGeneratedNever().IsRequired(true);
        builder.Entity<E>().Property(p => p.TaleId).IsRequired(true);
        builder.Entity<E>().Property(p => p.TaleVersionId).IsRequired(true);
        builder.Entity<E>().Property(p => p.Type).IsRequired(true);
        builder.Entity<E>().HasIndex(p => p.TaleId);
        builder.Entity<E>().HasIndex(p => new { p.TaleId, p.TaleVersionId, p.Id, p.Type });
        return builder;
    }
}
