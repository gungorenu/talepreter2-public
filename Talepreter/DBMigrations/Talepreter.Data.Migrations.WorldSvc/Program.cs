using Talepreter.Data.DbContext.WorldSvc;
using Talepreter.Data.Migrations.Base;
using Talepreter.Extensions;

DBMigrationHost.ExecuteMigrations<TaskDbContext>(ServiceId.WorldSvc);
