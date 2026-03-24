using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController(INotificationFeature notificationFeature) : BaseController
{
    [HttpPost("poke/{targetUserId:guid}")]
    public async Task<IActionResult> Poke(Guid targetUserId)
    {
        try
        {
            var result = await notificationFeature.PokeUserAsync(GetUserId(), targetUserId);
            return result.Status == ResultStatus.Success ? Ok() : BadRequest(result);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (EntityNotFoundException) { return NotFound("User not found"); }
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        try
        {
            var result = await notificationFeature.GetUnreadAsync(GetUserId());
            return Ok(result.Dto);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId)
    {
        try
        {
            var result = await notificationFeature.MarkReadAsync(GetUserId(), notificationId);
            return result.Status switch
            {
                ResultStatus.Success => NoContent(),
                ResultStatus.Unauthorized => Forbid(),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (EntityNotFoundException) { return NotFound(); }
    }
}