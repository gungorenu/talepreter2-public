using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Talepreter.Extensions;

namespace Talepreter.Data.Migrations.Base;

public static class DBMigrationHost
{
    public static void ExecuteMigrations<TDBContext>(ServiceId serviceId)
        where TDBContext : DbContext
    {
        ServiceStarter.StartService(() =>
        {
            Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddSerilog();
                    services.AddSingleton<ITalepreterServiceIdentifier>(_ => new TalepreterServiceIdentifier(serviceId));
                    services.AddDbContextFactory<TDBContext>();
                    services.AddHostedService<Migrator<TDBContext>>();
                })
                .Build()
                .Run();
        });
    }
}
