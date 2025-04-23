using Microsoft.EntityFrameworkCore;
using Talepreter.Common;

namespace Talepreter.OrleansClustering.DBContext
{
    public class OrleansClusteringDbContext : DbContext
    {
        public OrleansClusteringDbContext() { }
        public OrleansClusteringDbContext(DbContextOptions<OrleansClusteringDbContext> contextOptions) : base(contextOptions) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(EnvironmentVariableHandler.ReadEnvVar("OrleansClusteringDBConnection"), b => b.MigrationsAssembly("Talepreter.Data.Migrations.OrleansClustering"));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
