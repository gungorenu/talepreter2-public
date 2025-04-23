using Microsoft.EntityFrameworkCore;
using Serilog;
using Talepreter.Common;
using Talepreter.Common.Orleans;
using Talepreter.Common.RabbitMQ;
using Talepreter.Extensions;
using Talepreter.Operations;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Data.BaseTypes;
using Talepreter.Operations.Adventure.Validators;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Operations.Adventure.Executors;
using Talepreter.Data.DbContext.WorldSvc;
using Talepreter.Operations.Workload;
using CommandProcessor = Talepreter.Operations.Adventure.Processors.WorldSvcCommandProcessor;
using GrainFetcher = Talepreter.Operations.Adventure.GrainFetchers.WorldSvcGrainFetcher;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Messaging;

ServiceStarter.StartService(() =>
{
    var host = Host.CreateDefaultBuilder(args)
        .UseOrleans(silo => silo.ConfigureTalepreterOrleans("WorldSvcStorage", "WorldSilo", (int)ServiceId.WorldSvc))
        .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSerilog();
            services.RegisterTalepreterService(ServiceId.WorldSvc);
            services.AddDbContext<TaskDbContext>(options => options.UseSqlServer(EnvironmentVariableHandler.ReadEnvVar("DBConnection")));
            services.AddScoped<ITaskDbContext, TaskDbContext>();
            services.AddHostedService<BaseHostedService>();
            services.RegisterRabbitMQ();
            services.AddTransient<ICommandProcessor, CommandProcessor>();
            services.AddTransient<IBatchCommandProcessor, CommandProcessor>();
            services.AddTransient<ICommandValidator, CommandValidator>();
            services.AddSingleton<IDocumentDbContext, DocumentDbContext>();
            services.AddTransient<IExecuteTaskGrainFetcher, GrainFetcher>();
            services.AddTransient<ProcessTask>();
            services.AddTransient<ExecuteTask>();
            services.AddSingleton<IWorkTaskManager, WorkTaskManager>();
            services.AddHostedService<WorkerService>();
            services.AddTransient<IConsumer<ProcessPageRequest>, WorkerConsumer>();
            services.AddTransient<IConsumer<ExecutePageRequest>, WorkerConsumer>();
            services.AddTransient<IConsumer<CancelPageOperationRequest>, WorkerConsumer>();

            services.AddTransient<ICommandExecutor<ICacheGrain>, CacheGrainExecutor>();
            services.AddTransient<ICommandExecutor<IFactionGrain>, FactionGrainExecutor>();
            services.AddTransient<ICommandExecutor<IGroupGrain>, GroupGrainExecutor>();
            services.AddTransient<ICommandExecutor<IRaceGrain>, RaceGrainExecutor>();
            services.AddTransient<ICommandExecutor<ISettlementGrain>, SettlementGrainExecutor>();
            services.AddTransient<ICommandExecutor<IWorldGrain>, WorldGrainExecutor>();
            services.AddTransient<ITriggerExecutor<ISettlementGrain>, SettlementGrainExecutor>();
        })
        .Build();

    host.Run();
});

