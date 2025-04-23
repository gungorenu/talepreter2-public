using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Talepreter.Data.Migrations.Base;

public class MigrationDesignTimeFactory<TDBContext> : IDesignTimeDbContextFactory<TDBContext>
    where TDBContext : DbContext, new()
{
    public TDBContext CreateDbContext(string[] args)
    {
        return new TDBContext();
    }
}
