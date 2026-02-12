# Mqtt.Controllers

ASP.NET-style controllers for MQTT. Subscribe to topics using attributes, with full dependency injection support.

## Installation

```bash
dotnet add package Mqtt.Controllers
```

## Getting Started

Open `simulated-weather-station.html` in a browser to start a simulated IoT weather station that publishes telemetry to a public MQTT broker. Then run the example app to receive the data.

### Setup

```cs
// ExampleApp.GettingStarted/Program.cs

using Mqtt.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMqttControllers();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

var mqtt = app.Services.GetRequiredService<IMqttClientService>();
await mqtt.ConnectAsync("broker.hivemq.com", 1883);

app.Run();

```

### Controller

```cs
// ExampleApp.GettingStarted/WeatherStationController.cs

using Mqtt.Controllers;

namespace ExampleApp.GettingStarted;

public class WeatherStationController(ILogger<WeatherStationController> logger) : MqttController
{
    [MqttRoute("station/+/sensor/{sensorId}/telemetry")]
    public Task HandleTelemetry(string sensorId, SensorTelemetry data)
    {
        logger.LogInformation("{Sensor} | {Temp}C | {Humidity}% | {Pressure} hPa",
            sensorId, data.Temperature, data.Humidity, data.Pressure);
        return Task.CompletedTask;
    }
}

public record SensorTelemetry(
    string SensorId,
    string SensorName,
    string StationId,
    DateTime Timestamp,
    double Temperature,
    double Humidity,
    double Pressure,
    int LightLevel,
    string Status);

```

### Run it

```bash
dotnet run --project ExampleApp.GettingStarted
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

## Publishing Messages

Inject `IMqttClientService` anywhere to publish. This example exposes an HTTP endpoint that forwards commands to the simulated weather station:

```cs
// ExampleApp.GettingStarted/WebClientController.cs

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Mqtt.Controllers;

namespace ExampleApp.GettingStarted;

[ApiController]
[Route("api/[controller]")]
public class WebClientController(IMqttClientService mqtt) : ControllerBase
{
    [HttpPost("{sensorId}/command")]
    public async Task<IActionResult> SendCommand(string sensorId, [FromBody] JsonElement command)
    {
        await mqtt.PublishAsync($"station/aaa/sensor/{sensorId}/command", command.GetRawText());
        return Ok();
    }
}

```

```bash
curl -X POST http://localhost:5000/api/webclient/sensor-outdoor/command \
  -H 'Content-Type: application/json' \
  -d '{"action":"setInterval","value":10}'
```

## License

MIT
