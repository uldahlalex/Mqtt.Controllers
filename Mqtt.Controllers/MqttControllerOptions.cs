using System.Reflection;

namespace Mqtt.Controllers;

/// <summary>
/// Configuration options for MQTT controllers.
/// </summary>
public class MqttControllerOptions
{
    /// <summary>
    /// Assemblies to scan for MQTT controllers.
    /// </summary>
    public List<Assembly> Assemblies { get; } = new();
}
