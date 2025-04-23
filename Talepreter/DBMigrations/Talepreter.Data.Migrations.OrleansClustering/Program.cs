using Talepreter.Data.Migrations.Base;
using Talepreter.Extensions;
using Talepreter.OrleansClustering.DBContext;

DBMigrationHost.ExecuteMigrations<OrleansClusteringDbContext>(ServiceId.OrleansClustering);
