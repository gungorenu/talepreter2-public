using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Talepreter.Extensions;

namespace Talepreter.Data.Migrations.Base;

public class Migrator<TDBContext> : BackgroundService
    where TDBContext : DbContext
{
    private readonly IDbContextFactory<TDBContext> _dbContextFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Migrator> _logger;
    private readonly ITalepreterServiceIdentifier _svcIdentifier;

    public Migrator(IHostApplicationLifetime hostApplicationLifetime, ILogger<Migrator> logger, IDbContextFactory<TDBContext> factory, ITalepreterServiceIdentifier svcIdentifier)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _dbContextFactory = factory;
        _logger = logger;
        _svcIdentifier = svcIdentifier;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Done migration runner!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Migration runner error:{ex.Message} | {ex.StackTrace} | {ex}");
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting migration runner for [{_svcIdentifier.Name}]!");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Stoping migration runner for [{_svcIdentifier.Name}]!");
        await base.StopAsync(cancellationToken);
    }
}
