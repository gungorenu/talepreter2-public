using Serilog;
using Talepreter.Common;
using Talepreter.Common.Orleans;
using Talepreter.Common.RabbitMQ;
using Talepreter.Data.DocumentDbContext;
using Talepreter.Extensions;
using Talepreter.TaleSvc;

ServiceStarter.StartService(() =>
{
    var builder = WebApplication.CreateBuilder(args);
    builder.UseOrleans(silo => silo.ConfigureTalepreterOrleans("TaleSvcStorage", "TaleSilo", (int)ServiceId.TaleSvc));
    builder.WebHost.UseKestrel();
    builder.WebHost.UseUrls(
        $"http://*:{EnvironmentVariableHandler.ReadEnvVar("ASPNETCORE_HTTP_PORT")}",
        $"https://*:{EnvironmentVariableHandler.ReadEnvVar("ASPNETCORE_HTTPS_PORT")}");

    var services = builder.Services;
    services.AddLogging();
    services.AddSerilog();
    services.RegisterTalepreterService(ServiceId.TaleSvc);
    services.AddHostedService<BaseHostedService>();
    services.RegisterRabbitMQ();
    services.AddHostedService<ExchangeSetupService>();
    services.AddSingleton<IDocumentDbContext, DocumentDbContext>();

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddControllersWithViews();
    services.AddSwaggerGen();

    var app = builder.Build();
    if (EnvironmentVariableHandler.ReadEnvVar("TaleSvcEnvironment") == "DEV")
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    /* generates openapi file
    var swaggerProvider = app.Services.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
    var swagger = swaggerProvider.GetSwagger("v1");
    var stringWriter = new StringWriter();
    swagger.SerializeAsV3(new Microsoft.OpenApi.Writers.OpenApiYamlWriter(stringWriter));
    var swaggerYaml = stringWriter.ToString(); // << file here, write to somewhere
    */

    app.Run();
});
