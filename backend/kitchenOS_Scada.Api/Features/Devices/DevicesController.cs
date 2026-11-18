using kitchenOS.Simulator;
using kitchenOS_Scada.Api.Simulator.Devices;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Features.Devices;

[ApiController]
[Route("devices")]
public sealed class DevicesController(
    DeviceSimulator simulator,
    IDeviceCommandService commandService,
    ILogger<DevicesController> logger) : ControllerBase
{
    /// <summary>Returns all simulated devices and their current points.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DeviceResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DeviceResponse>> GetDevices()
    {
        IReadOnlyList<DeviceResponse> devices = simulator.GetDevices()
            .Select(DeviceDtoMappings.ToResponse)
            .ToArray();

        return Ok(devices);
    }

    /// <summary>Rejects command requests that do not specify a device ID.</summary>
    [HttpPost("commands")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SendCommandWithoutDeviceId() =>
        BadRequest(new { error = "Device ID is required." });

    /// <summary>Queues a command for a simulated device.</summary>
    [HttpPost("{deviceId}/commands")]
    [ProducesResponseType<QueuedDeviceCommandResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<QueuedDeviceCommandResponse>> SendCommand(
        string deviceId,
        SendDeviceCommandRequest? request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return BadRequest(new { error = "Device ID is required." });
        }

        if (request is null)
        {
            return BadRequest(new { error = "A command request is required." });
        }

        try
        {
            DeviceCommandSubmissionResult result = await commandService.SubmitAsync(
                deviceId,
                request,
                cancellationToken);

            if (result.DeviceNotFound)
            {
                return NotFound(new { error = result.Error });
            }

            if (result.Error is not null)
            {
                return BadRequest(new { error = result.Error });
            }

            QueuedDeviceCommandResponse response = new(
                result.Command!.CommandId,
                result.Command.DeviceId,
                "queued");

            return Accepted(response);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            logger.LogError(exception, "Could not queue command for device {DeviceId}", deviceId);
            return Problem(
                "The command bus is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
