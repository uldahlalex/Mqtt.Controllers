using System.Security;
using HiveMQtt.Client;
using HiveMQtt.MQTT5.Types;

namespace Mqtt.Controllers;

/// <summary>
/// HiveMQ-based implementation of <see cref="IMqttClientService"/>.
/// </summary>
public class MqttClientService : IMqttClientService, IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly Dictionary<string, List<Func<string, string, Task>>> _handlers = new();
    private HiveMQClient? _client;

    /// <inheritdoc />
    public bool IsConnected => _client?.IsConnected() ?? false;

    /// <inheritdoc />
    public async Task ConnectAsync(string host, int port = 1883, string? username = null, string? password = null, bool? useTls = null)
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected) return;

            var optionsBuilder = new HiveMQClientOptionsBuilder()
                .WithBroker(host)
                .WithPort(port);

            if (useTls ?? port == 8883)
                optionsBuilder.WithUseTls(true);

            if (!string.IsNullOrEmpty(username))
            {
                optionsBuilder.WithUserName(username);
                if (!string.IsNullOrEmpty(password))
                {
                    var securePassword = new SecureString();
                    foreach (var c in password) securePassword.AppendChar(c);
                    optionsBuilder.WithPassword(securePassword);
                }
            }

            _client = new HiveMQClient(optionsBuilder.Build());

            _client.OnMessageReceived += async (_, args) =>
            {
                var topic = args.PublishMessage.Topic ?? "";
                var payload = args.PublishMessage.PayloadAsString;

                foreach (var (pattern, handlers) in _handlers)
                {
                    if (TopicMatches(pattern, topic))
                    {
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                await handler(topic, payload);
                            }
                            catch
                            {
                                // Handler exceptions should not crash the receive loop
                            }
                        }
                    }
                }
            };

            await _client.ConnectAsync().ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(string topic, string payload)
    {
        if (_client == null || !IsConnected)
            throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync first.");

        await _client.PublishAsync(topic, payload).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(string topic)
    {
        if (_client == null || !IsConnected)
            throw new InvalidOperationException("MQTT client is not connected. Call ConnectAsync first.");

        if (!_handlers.ContainsKey(topic))
        {
            _handlers[topic] = new List<Func<string, string, Task>>();

            var subscribeOptions = new SubscribeOptionsBuilder()
                .WithSubscription(topic, QualityOfService.AtLeastOnceDelivery)
                .Build();

            await _client.SubscribeAsync(subscribeOptions).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void RegisterHandler(string topicPattern, Func<string, string, Task> handler)
    {
        if (!_handlers.ContainsKey(topicPattern))
            _handlers[topicPattern] = new List<Func<string, string, Task>>();

        _handlers[topicPattern].Add(handler);
    }

    private static bool TopicMatches(string pattern, string topic)
    {
        var patternParts = pattern.Split('/');
        var topicParts = topic.Split('/');

        for (int i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i] == "#") return true;
            if (i >= topicParts.Length) return false;
            if (patternParts[i] == "+") continue;
            if (patternParts[i] != topicParts[i]) return false;
        }

        return patternParts.Length == topicParts.Length;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client?.DisconnectAsync().GetAwaiter().GetResult();
        _client?.Dispose();
        _connectionLock.Dispose();
    }
}
