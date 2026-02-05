namespace Mqtt.Controllers;

/// <summary>
/// Base class for MQTT controllers. Inherit from this class and add methods with <see cref="MqttRouteAttribute"/>.
/// </summary>
public abstract class MqttController;

/// <summary>
/// Marks a method as an MQTT message handler for a specific topic pattern.
/// </summary>
/// <remarks>
/// Supports MQTT wildcards:
/// <list type="bullet">
///   <item><c>+</c> - Single level wildcard (matches one topic level)</item>
///   <item><c>#</c> - Multi-level wildcard (matches remaining levels, must be last)</item>
/// </list>
/// Also supports named parameters using <c>{paramName}</c> syntax, which extracts the value
/// and binds it to a method parameter with the same name.
/// </remarks>
/// <example>
/// <code>
/// [MqttRoute("devices/{deviceId}/telemetry")]
/// public async Task HandleTelemetry(string deviceId, TelemetryDto data)
/// {
///     // deviceId is extracted from the topic
///     // data is deserialized from the JSON payload
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class MqttRouteAttribute : Attribute
{
    /// <summary>
    /// The topic pattern to subscribe to.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Creates a new MQTT route attribute.
    /// </summary>
    /// <param name="topic">The topic pattern (supports +, #, and {param} syntax).</param>
    public MqttRouteAttribute(string topic)
    {
        Topic = topic;
    }
}
