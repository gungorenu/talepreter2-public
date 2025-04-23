using Microsoft.EntityFrameworkCore;
using Serilog;
using Talepreter.Common;
using Talepreter.Common.Orleans;
using Talepreter.Common.RabbitMQ;
using Talepreter.Extensions;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Operations;
using Talepreter.Operations.Adventure.Validators;
using Talepreter.Data.BaseTypes;
using Talepreter.Contracts.Orleans.Grains;
using Talepreter.Operations.Adventure.Executors;
using Talepreter.Data.DbContext.ActorSvc;
using Talepreter.Operations.Workload;
using CommandProcessor = Talepreter.Operations.Adventure.Processors.ActorSvcCommandProcessor;
using GrainFetcher = Talepreter.Operations.Adventure.GrainFetchers.ActorSvcGrainFetcher;
using Talepreter.Contracts.Messaging;
using Talepreter.Common.RabbitMQ.Interfaces;

ServiceStarter.StartService(() =>
{
    var host = Host.CreateDefaultBuilder(args)
        .UseOrleans(silo => silo.ConfigureTalepreterOrleans("ActorSvcStorage", "ActorSilo", (int)ServiceId.ActorSvc))
        .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSerilog();
            services.RegisterTalepreterService(ServiceId.ActorSvc);
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

            services.AddTransient<ICommandExecutor<IActorGrain>, ActorGrainExecutor>();
            services.AddTransient<ITriggerExecutor<IActorGrain>, ActorGrainExecutor>();
            services.AddTransient<ICommandExecutor<ICohortGrain>, CohortGrainExecutor>();
            services.AddTransient<ITriggerExecutor<ICohortGrain>, CohortGrainExecutor>();
            services.AddTransient<ICommandExecutor<IEquipmentGrain>, EquipmentGrainExecutor>();
            services.AddTransient<ICommandExecutor<IActorExtensionGrain>, ActorExtensionGrainExecutor>();
        })
        .Build();

    host.Run();
});