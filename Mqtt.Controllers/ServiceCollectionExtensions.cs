using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mqtt.Controllers;

/// <summary>
/// Extension methods for registering MQTT controllers with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MQTT controller services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for MQTT controllers. If empty, uses the calling assembly.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMqttControllers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var options = new MqttControllerOptions();

        if (assemblies.Length == 0)
            options.Assemblies.Add(Assembly.GetCallingAssembly());
        else
            options.Assemblies.AddRange(assemblies);

        services.AddSingleton(options);
        services.AddSingleton<IMqttClientService, MqttClientService>();
        services.AddHostedService<MqttControllerHostedService>();

        return services;
    }
}
