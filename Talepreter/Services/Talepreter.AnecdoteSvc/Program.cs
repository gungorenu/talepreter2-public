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
using Talepreter.Data.DbContext.AnecdoteSvc;
using Talepreter.Operations.Workload;
using CommandProcessor = Talepreter.Operations.Adventure.Processors.AnecdoteSvcCommandProcessor;
using GrainFetcher = Talepreter.Operations.Adventure.GrainFetchers.AnecdoteSvcGrainFetcher;
using Talepreter.Common.RabbitMQ.Interfaces;
using Talepreter.Contracts.Messaging;

ServiceStarter.StartService(() =>
{
    var host = Host.CreateDefaultBuilder(args)
        .UseOrleans(silo => silo.ConfigureTalepreterOrleans("AnecdoteSvcStorage", "AnecdoteSilo", (int)ServiceId.AnecdoteSvc))
        .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSerilog();
            services.RegisterTalepreterService(ServiceId.AnecdoteSvc);
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

            services.AddTransient<ICommandExecutor<IAnecdoteGrain>, AnecdoteGrainExecutor>();
            services.AddTransient<ICommandExecutor<IAnecdoteExtensionGrain>, AnecdoteExtensionGrainExecutor>();
        })
        .Build();

    host.Run();
});