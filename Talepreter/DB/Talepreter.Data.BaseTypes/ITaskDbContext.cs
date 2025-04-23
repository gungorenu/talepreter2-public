using Microsoft.EntityFrameworkCore;

namespace Talepreter.Data.BaseTypes;

public interface ITaskDbContext : IDisposable
{
    DbSet<Command> Commands { get; }
    DbSet<Trigger> Triggers { get; }

    Task<int> SaveChangesAsync(CancellationToken token);
    Task<int> ExecuteAwaitingCommandsMaxPhase(Guid taleId, Guid taleVersionId, int chapter, int page, CancellationToken token);
    IQueryable<Command> ExecuteAwaitingCommands(Guid taleId, Guid taleVersionId, int chapter, int page, int phase);
    IQueryable<Trigger> GetActiveTriggersBefore(Guid taleId, Guid taleVersionId, long date);
    Task<int> DeleteTriggerAsync(Guid taleId, Guid taleVersionId, string id, CancellationToken token);
    Task<int> ShiftTriggerAsync(Guid taleId, Guid taleVersionId, string id, long newTime, CancellationToken token);
    Task<int> UpdateTriggerAsync(Guid taleId, Guid taleVersionId, string id, TriggerState state, CancellationToken token);
    Task PurgeEntitiesAsync(Guid taleId, Guid? taleVersionId, CancellationToken token);
    Task<int> BackupToAsync(Guid taleId, Guid taleVersionId, Guid newVersionId, CancellationToken token);
}
