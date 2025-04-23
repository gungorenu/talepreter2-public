using Microsoft.EntityFrameworkCore;
using Talepreter.Common;

namespace Talepreter.Data.Migrations.TaleSvc;

public class TaleSvcDbContext : DbContext
{
    public TaleSvcDbContext() { }
    public TaleSvcDbContext(DbContextOptions<TaleSvcDbContext> contextOptions) : base(contextOptions) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(EnvironmentVariableHandler.ReadEnvVar("DBConnection"), b => b.MigrationsAssembly("Talepreter.Data.Migrations.TaleSvc"));
        base.OnConfiguring(optionsBuilder);
    }
}