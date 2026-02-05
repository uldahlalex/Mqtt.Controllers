namespace Mqtt.Controllers;

/// <summary>
/// MQTT client service for connecting to brokers, subscribing to topics, and publishing messages.
/// </summary>
public interface IMqttClientService
{
    /// <summary>
    /// Whether the client is currently connected to the broker.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to an MQTT broker.
    /// </summary>
    /// <param name="host">Broker hostname or IP address.</param>
    /// <param name="port">Broker port (typically 1883 for TCP, 8883 for TLS).</param>
    /// <param name="username">Optional username for authentication.</param>
    /// <param name="password">Optional password for authentication.</param>
    /// <param name="useTls">Whether to use TLS encryption.</param>
    Task ConnectAsync(string host, int port = 1883, string? username = null, string? password = null, bool? useTls = null);

    /// <summary>
    /// Publishes a message to a topic.
    /// </summary>
    /// <param name="topic">The topic to publish to.</param>
    /// <param name="payload">The message payload.</param>
    Task PublishAsync(string topic, string payload);

    /// <summary>
    /// Subscribes to a topic pattern.
    /// </summary>
    /// <param name="topic">The topic pattern (supports + and # wildcards).</param>
    Task SubscribeAsync(string topic);

    /// <summary>
    /// Registers a handler for messages matching a topic pattern.
    /// </summary>
    /// <param name="topicPattern">The topic pattern (supports + and # wildcards).</param>
    /// <param name="handler">Handler function receiving (topic, payload).</param>
    void RegisterHandler(string topicPattern, Func<string, string, Task> handler);
}
