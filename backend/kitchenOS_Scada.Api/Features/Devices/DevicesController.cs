using kitchenOS_Scada.Api.Features.Devices.Contracts;
using kitchenOS_Scada.Api.Features.Devices.Queries;
using kitchenOS_Scada.Api.Features.Devices.Registration;
using kitchenOS_Scada.Api.Features.Devices.Removal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using kitchenOS_Scada.Api.Security;
using StackExchange.Redis;

namespace kitchenOS_Scada.Api.Features.Devices;

[Authorize(Policy = AuthorizationPolicies.Monitor)]
[ApiController]
[Route("devices")]
public sealed class DevicesController : ControllerBase
{
    private readonly IDeviceQueryService _queryService;
    private readonly IDeviceRegistrationService _registrationService;
    private readonly IDeviceRemovalService _removalService;
    private readonly IDeviceCommandService _commandService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        IDeviceQueryService queryService,
        IDeviceRegistrationService registrationService,
        IDeviceRemovalService removalService,
        IDeviceCommandService commandService,
        ILogger<DevicesController> logger)
    {
        _queryService = queryService;
        _registrationService = registrationService;
        _removalService = removalService;
        _commandService = commandService;
        _logger = logger;
    }

    /// <summary>Returns all active devices in the persisted inventory.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DeviceInventoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceInventoryResponse>>> GetDevices(
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<DeviceInventoryResponse> devices =
                await _queryService.GetActiveAsync(cancellationToken);

            return Ok(devices);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Error retrieving active devices");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving active devices." });
        }
    }

    /// <summary>Returns one device from the persisted inventory.</summary>
    [HttpGet("{deviceId}")]
    [ProducesResponseType<DeviceInventoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<DeviceInventoryResponse>> GetDevice(
        string deviceId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>Registers a device in the persisted inventory.</summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Administer)]
    [ProducesResponseType<DeviceInventoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<ActionResult<DeviceInventoryResponse>> RegisterDevice(
        RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>Removes a device from the active persisted inventory.</summary>
    [HttpDelete("{deviceId}")]
    [Authorize(Policy = AuthorizationPolicies.Administer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> RemoveDevice(
        string deviceId,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>Rejects command requests that do not specify a device ID.</summary>
    [HttpPost("commands")]
    [Authorize(Policy = AuthorizationPolicies.Operate)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SendCommandWithoutDeviceId()
    {
        return BadRequest(new { error = "Device ID is required." });
    }

    /// <summary>Queues a command for a simulated device.</summary>
    [HttpPost("{deviceId}/commands")]
    [Authorize(Policy = AuthorizationPolicies.Operate)]
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
            DeviceCommandSubmissionResult result = await _commandService.SubmitAsync(
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
            _logger.LogError(exception, "Could not queue command for device {DeviceId}", deviceId);
            return Problem(
                "The command bus is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}

