using Microsoft.EntityFrameworkCore;
using Talepreter.Common;
using Talepreter.Data.BaseTypes;

namespace Talepreter.Data.DbContext.WorldSvc;

public class TaskDbContext : TaskDbContextBase, ITaskDbContext
{
    public TaskDbContext() { }
    public TaskDbContext(DbContextOptions contextOptions) : base(contextOptions) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(EnvironmentVariableHandler.ReadEnvVar("DBConnection"), b => 
        { 
            b.MigrationsAssembly("Talepreter.Data.Migrations.WorldSvc"); 
            b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null); 
        });
        base.OnConfiguring(optionsBuilder);
    }
}
