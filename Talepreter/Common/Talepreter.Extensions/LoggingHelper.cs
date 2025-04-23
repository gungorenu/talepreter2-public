using Serilog;
using Talepreter.Common;

namespace Talepreter.Extensions;

public static class LoggingHelper
{
    public static void SetupSerilog()
    {
        // reader and consumer logs are same
        var logQueueReaders = EnvironmentVariableHandler.TryReadEnvVar("LoggingQueueReaders");
        var queueReaderLogging = Serilog.Events.LogEventLevel.Debug;
        if (logQueueReaders != null) _ = Enum.TryParse(logQueueReaders, out queueReaderLogging);

        var logQueuePublishers = EnvironmentVariableHandler.TryReadEnvVar("LoggingQueuePublishers");
        var queuePublisherLogging = Serilog.Events.LogEventLevel.Debug;
        if (logQueuePublishers != null) _ = Enum.TryParse(logQueuePublishers, out queuePublisherLogging);

        var logCommandProcessors = EnvironmentVariableHandler.TryReadEnvVar("LoggingCommandProcessors");
        var commandProcessorLogging = Serilog.Events.LogEventLevel.Debug;
        if (logCommandProcessors != null) _ = Enum.TryParse(logCommandProcessors, out commandProcessorLogging);

        var logApi = EnvironmentVariableHandler.TryReadEnvVar("LoggingApi");
        var apiLogging = Serilog.Events.LogEventLevel.Debug;
        if (logApi != null) _ = Enum.TryParse(logApi, out apiLogging);

        var logGrains = EnvironmentVariableHandler.TryReadEnvVar("LoggingGrains");
        var grainsLogging = Serilog.Events.LogEventLevel.Debug;
        if (logGrains != null) _ = Enum.TryParse(logGrains, out grainsLogging);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Orleans", Serilog.Events.LogEventLevel.Warning)

            // rabbitmq stuff
            .MinimumLevel.Override("Talepreter.Common.RabbitMQ.Consumer.RabbitMQMessageReaderService", queueReaderLogging)
            .MinimumLevel.Override("Talepreter.Common.RabbitMQ.Interfaces.IPublisher", queuePublisherLogging)

            // processing & executing
            .MinimumLevel.Override("Talepreter.Operations.Workload", commandProcessorLogging)

            // api
            .MinimumLevel.Override("Talepreter.TaleSvc.TaleController", apiLogging)

            // grains
            .MinimumLevel.Override("Talepreter.Operations.Grains", grainsLogging)
            .MinimumLevel.Override("Talepreter.TaleSvc.Grains", grainsLogging)
            .MinimumLevel.Override("Talepreter.ActorSvc.Grains", grainsLogging)
            .MinimumLevel.Override("Talepreter.WorldSvc.Grains", grainsLogging)
            .MinimumLevel.Override("Talepreter.PersonSvc.Grains", grainsLogging)
            .MinimumLevel.Override("Talepreter.AnecdoteSvc.Grains", grainsLogging)

            .Filter.ByExcluding("IsMatch( @m, 'CleanupDefunctSilos' )")

            .WriteTo.Console()
            .CreateLogger();

        Log.Information(new string('-', 144));
        Log.Information(new string('-', 12) + $" STARTING UP " + new string('-', 119));
        Log.Information(new string('-', 144));
    }
}
