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
