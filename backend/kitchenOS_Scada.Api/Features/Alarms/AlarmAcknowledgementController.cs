using System.Security.Claims;
using kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;
using kitchenOS_Scada.Api.Features.Alarms.Contracts;
using kitchenOS_Scada.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kitchenOS_Scada.Api.Features.Alarms;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Monitor)]
[Route("alarms")]
public sealed class AlarmAcknowledgementController(
    IAlarmAcknowledgementService alarmAcknowledgementService) : ControllerBase
{
    /// <summary>Returns persisted alarms, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AlarmStatusResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlarmStatusResponse>>> GetRecent(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AlarmStatusResponse> alarms =
            await alarmAcknowledgementService.GetRecentAsync(cancellationToken);
        return Ok(alarms);
    }

    /// <summary>Acknowledges existing occurrences of the selected alarm code for that device on behalf of the authenticated operator.</summary>
    [HttpPost("acknowledgements")]
    [Authorize(Policy = AuthorizationPolicies.Operate)]
    [ProducesResponseType<AlarmAcknowledgementResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlarmAcknowledgementResponse>> Acknowledge(
        AcknowledgeAlarmRequest request,
        CancellationToken cancellationToken)
    {
        string? operatorId = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(operatorId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Operator identity is unavailable.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        string operatorName = User.Identity?.Name ?? operatorId;
        AlarmAcknowledgementResult result =
            await alarmAcknowledgementService.AcknowledgeAsync(
                request,
                operatorId,
                operatorName,
                cancellationToken);

        return result.Status switch
        {
            AlarmAcknowledgementResultStatus.Acknowledged or
                AlarmAcknowledgementResultStatus.AlreadyAcknowledged =>
                Ok(result.Acknowledgement),
            AlarmAcknowledgementResultStatus.NotFound => NotFound(new ProblemDetails
            {
                Title = "Alarm not found.",
                Detail = $"No alarm with ID {request.AlarmId} exists.",
                Status = StatusCodes.Status404NotFound
            }),
            AlarmAcknowledgementResultStatus.DeviceMismatch => BadRequest(new ProblemDetails
            {
                Title = "Alarm and device do not match.",
                Detail = "The device ID does not match the identified alarm.",
                Status = StatusCodes.Status400BadRequest
            }),
            _ => BadRequest(new ProblemDetails
            {
                Title = "Invalid acknowledgement request.",
                Status = StatusCodes.Status400BadRequest
            })
        };
    }
}
