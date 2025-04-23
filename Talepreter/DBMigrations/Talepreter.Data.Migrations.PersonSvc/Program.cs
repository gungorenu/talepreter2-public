using Talepreter.Data.DbContext.PersonSvc;
using Talepreter.Data.Migrations.Base;
using Talepreter.Extensions;

DBMigrationHost.ExecuteMigrations<TaskDbContext>(ServiceId.PersonSvc);
