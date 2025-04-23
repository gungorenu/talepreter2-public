using Talepreter.Data.Migrations.Base;
using Talepreter.Data.Migrations.TaleSvc;
using Talepreter.Extensions;

DBMigrationHost.ExecuteMigrations<TaleSvcDbContext>(ServiceId.TaleSvc);
