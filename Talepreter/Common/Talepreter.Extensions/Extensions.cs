using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Talepreter.Extensions;

public static class Extensions
{
    /// <summary>
    /// Registers service identifier and base hosted service, for logging and service identification purposes
    /// </summary>
    public static IServiceCollection RegisterTalepreterService(this IServiceCollection services, ServiceId serviceId)
    {
        services.AddSingleton<ITalepreterServiceIdentifier>(_ => new TalepreterServiceIdentifier(serviceId));
        services.AddHostedService<BaseHostedService>();
        return services;
    }

    public static string ServiceIdTo(this ServiceId serviceId)
        => serviceId switch
        {
            ServiceId.ActorSvc => "Actor-Svc",
            ServiceId.AnecdoteSvc => "Anecdote-Svc",
            ServiceId.PersonSvc => "Person-Svc",
            ServiceId.WorldSvc => "World-Svc",
            ServiceId.TaleSvc => "Tale-Svc",
            ServiceId.OrleansClustering => "OrleansClustering-Migr",
            _ => throw new InvalidEnumArgumentException($"Service id {serviceId} is not recognized")
        };

    public static T? Clone<T>(this T? source, Action<T, T> assign)
        where T : class, new()
    {
        if (source == null) return null!;
        var result = new T();
        assign(source!, result);
        return result;
    }
}
