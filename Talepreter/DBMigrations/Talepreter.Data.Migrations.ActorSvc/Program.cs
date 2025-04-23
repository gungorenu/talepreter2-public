using Talepreter.Data.DbContext.ActorSvc;
using Talepreter.Data.Migrations.Base;
using Talepreter.Extensions;

DBMigrationHost.ExecuteMigrations<TaskDbContext>(ServiceId.ActorSvc);
