using Talepreter.Data.DbContext.AnecdoteSvc;
using Talepreter.Data.Migrations.Base;
using Talepreter.Extensions;

DBMigrationHost.ExecuteMigrations<TaskDbContext>(ServiceId.AnecdoteSvc);
