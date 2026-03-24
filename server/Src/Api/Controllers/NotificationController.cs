using Application.Common.Interfaces.Features;
using Application.Common.Interfaces.Services;
using Application.Common.Results;
using Application.DTOs.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController(INotificationFeature notificationFeature, IUserService userService) : BaseController
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

    [HttpGet]
    public async Task<IActionResult> GetUnread()
    {
        try
        {
            var result = await notificationFeature.GetUnreadAsync(GetUserId());
            return Ok(result.Dto);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }
    
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            return Ok(UserDto.Map(user));
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
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