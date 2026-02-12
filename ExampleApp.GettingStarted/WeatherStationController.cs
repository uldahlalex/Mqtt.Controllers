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
