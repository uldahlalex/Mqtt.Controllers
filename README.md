# Mqtt.Controllers

ASP.NET-style controllers for MQTT. Subscribe to topics using attributes, with full dependency injection support.

## Installation

```bash
dotnet add package Mqtt.Controllers
```

## Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMqttControllers();

var app = builder.Build();

var mqtt = app.Services.GetRequiredService<IMqttClientService>();
await mqtt.ConnectAsync("broker.example.com", 1883, "username", "password");

app.Run();
```

## Creating Controllers

Inherit from `MqttController` and use `[MqttRoute]` to subscribe to topics:

```csharp
public class DeviceController(ILogger<DeviceController> logger, MyDbContext db) : MqttController
{
    // Simple topic - payload is deserialized from JSON
    [MqttRoute("weather")]
    public async Task HandleWeather(WeatherData data)
    {
        logger.LogInformation("Temperature: {Temp}", data.Temperature);
    }

    // Topic with parameter - {deviceId} extracted from topic
    [MqttRoute("devices/{deviceId}/telemetry")]
    public async Task HandleTelemetry(string deviceId, TelemetryData data)
    {
        db.Readings.Add(new Reading { DeviceId = deviceId, Value = data.Value });
        await db.SaveChangesAsync();
    }

    // Raw payload access
    [MqttRoute("devices/{deviceId}/raw")]
    public Task HandleRaw(string deviceId, string payload)
    {
        logger.LogInformation("Device {Id} sent: {Payload}", deviceId, payload);
        return Task.CompletedTask;
    }
}

public record WeatherData(double Temperature, double Humidity);
public record TelemetryData(double Value, string Unit);
```

## Publishing Messages

Inject `IMqttClientService` anywhere to publish:

```csharp
[ApiController]
[Route("api/[controller]")]
public class CommandsController(IMqttClientService mqtt) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SendCommand([FromBody] Command cmd)
    {
        await mqtt.PublishAsync($"devices/{cmd.DeviceId}/commands", JsonSerializer.Serialize(cmd));
        return Ok();
    }
}
```

## Topic Patterns

| Pattern | Example Topic | Description |
|---------|---------------|-------------|
| `weather` | `weather` | Exact match |
| `devices/+/telemetry` | `devices/sensor1/telemetry` | `+` matches one level |
| `devices/#` | `devices/a/b/c` | `#` matches remaining levels |
| `devices/{id}/data` | `devices/sensor1/data` | `{id}` captures to parameter |

## Method Parameters

| Parameter | Source |
|-----------|--------|
| `string topic` | The full topic string |
| `string payload` | Raw message payload |
| `string {name}` | Captured from `{name}` in route |
| `MyDto data` | JSON-deserialized from payload |
| `CancellationToken` | Cancellation token |

## License

MIT
