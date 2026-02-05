using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mqtt.Controllers;

/// <summary>
/// Hosted service that discovers MQTT controllers and routes messages to them.
/// </summary>
public class MqttControllerHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IMqttClientService _mqtt;
    private readonly ILogger<MqttControllerHostedService> _logger;
    private readonly Assembly[] _assemblies;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Creates a new instance of the MQTT controller hosted service.
    /// </summary>
    public MqttControllerHostedService(
        IServiceProvider services,
        IMqttClientService mqtt,
        ILogger<MqttControllerHostedService> logger,
        MqttControllerOptions options)
    {
        _services = services;
        _mqtt = mqtt;
        _logger = logger;
        _assemblies = options.Assemblies.ToArray();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var controllerTypes = _assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(MqttController)) && !t.IsAbstract);

        var subscribedTopics = new HashSet<string>();

        foreach (var controllerType in controllerTypes)
        {
            foreach (var method in controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<MqttRouteAttribute>();
                if (attr == null) continue;

                var regex = TopicMatcher.PatternToRegex(attr.Topic);
                var subscribeTopic = TopicMatcher.PatternToSubscription(attr.Topic);

                if (subscribedTopics.Add(subscribeTopic))
                    await _mqtt.SubscribeAsync(subscribeTopic);

                var capturedMethod = method;
                var capturedControllerType = controllerType;
                var capturedRegex = regex;

                _mqtt.RegisterHandler(subscribeTopic, async (topic, payload) =>
                {
                    var match = capturedRegex.Match(topic);
                    if (match.Success)
                        await InvokeAsync(capturedControllerType, capturedMethod, topic, payload, match);
                });

                _logger.LogInformation("MQTT route: {Topic} -> {Controller}.{Method}",
                    subscribeTopic, controllerType.Name, method.Name);
            }
        }
    }

    private async Task InvokeAsync(Type controllerType, MethodInfo method, string topic, string payload, Match match)
    {
        using var scope = _services.CreateScope();
        var controller = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controllerType);

        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];

            // Route parameter from topic (e.g., {deviceId})
            if (p.Name != null && match.Groups[p.Name].Success)
            {
                args[i] = Convert.ChangeType(match.Groups[p.Name].Value, p.ParameterType);
            }
            // Raw topic string
            else if (p.ParameterType == typeof(string) && p.Name == "topic")
            {
                args[i] = topic;
            }
            // Raw payload string
            else if (p.ParameterType == typeof(string) && p.Name == "payload")
            {
                args[i] = payload;
            }
            // CancellationToken
            else if (p.ParameterType == typeof(CancellationToken))
            {
                args[i] = CancellationToken.None;
            }
            // Deserialize DTO from JSON payload
            else if (p.ParameterType.IsClass && p.ParameterType != typeof(string))
            {
                try
                {
                    args[i] = JsonSerializer.Deserialize(payload, p.ParameterType, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize payload to {Type}", p.ParameterType.Name);
                    args[i] = null;
                }
            }
        }

        var result = method.Invoke(controller, args);
        if (result is Task task) await task;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
