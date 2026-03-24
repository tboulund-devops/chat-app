using System.Security.Claims;
using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Chat;
using Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(
    IChatFeature chatFeature,
    IChatRoomRepository roomRepository,
    ISimpleSse backplane
) : BaseController
{

    

    /// <summary>
    /// SSE endpoint to subscribe to real-time messages in a chat room.
    [HttpGet("stream")]
    public async Task StreamMessages(CancellationToken cancellationToken)
    {
        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            Response.StatusCode = 401;
            return;
        }
        
        var (connectionId, channel) = backplane.Connect();
        try
        {
            // Configure SSE headers manually
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("X-Accel-Buffering", "no"); // important for nginx

            await Response.Body.FlushAsync(cancellationToken);

            // Send connected event
            await WriteSseEvent("connected", 
                $"Connection: {connectionId}", 
                cancellationToken);

            //Notifications
            await backplane.SubscribeUserAsync(connectionId, userId);
            await WriteSseEvent("connected", $"Connection: {connectionId}", cancellationToken);
            
            // Subscribe to rooms
            var clientRooms = await roomRepository.GetRoomsForUserAsync(userId);
            foreach (var room in clientRooms)
            {
                await backplane.AddToGroupAsync(connectionId, room.Id);
                await WriteSseEvent("Joined room", $"joined_room: {room.Id}", cancellationToken);
            }
            
            // Optional heartbeat
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await WriteSseEvent("ping", "keep-alive", cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                }
            }, cancellationToken);

            // Main channel loop
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await WriteSseEvent(
                    evt.Group.ToString()!,
                    evt.Data.GetRawText(),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected — normal
        }
        finally
        {
            await backplane.DisconnectAsync(connectionId);
        }
    }
    
    private async Task WriteSseEvent(
        string eventName,
        string data,
        CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
    
    /// <summary>
    /// Send a message to a chat room.
    /// </summary>
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var result = await chatFeature.CreateMessageAsync(GetUserId(), request);
            await backplane.SendToGroupAsync(request.RoomId, request);
            return result.Status switch
            {
                ResultStatus.Success => Ok(result.Dto),
                ResultStatus.Failure => BadRequest(result),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Get message history for a room.
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid roomId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }

        var result = await chatFeature.GetMessagesAsync(userId, roomId, skip, take);
        return result.Status switch
        {
            ResultStatus.Success => Ok(result),
            ResultStatus.Unauthorized => Forbid("You are not a member of this room"),
            ResultStatus.Failure => BadRequest(result),
            _ => BadRequest(result)
        };
    }

    /// <summary>
    /// Create a new chat room.
    /// </summary>
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var result = await chatFeature.CreateRoomAsync(GetUserId(), request);
            return result.Status switch
            {
                ResultStatus.Failure => BadRequest(result),
                ResultStatus.Success => CreatedAtAction(nameof(GetMessages), new { roomId = result.Dto!.Id }, result.Message),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Get all rooms in general
    /// </summary>
    ///
    [HttpGet("get-all-rooms")]
    public async Task<IActionResult> GetAllRooms()
    {
        var allRooms = await roomRepository.GetAllRoomsAsync();
        return Ok(allRooms);

    }

    /// <summary>
    /// Get all rooms client is member of
    /// </summary>
    [HttpGet("my-rooms")]
    public async Task<IActionResult> GetMyRooms()
    {
        Guid userId;
        try
        {
            userId = GetUserId();
        }
        catch (UnauthorizedAccessException e)
        {
            return Unauthorized();
        }
        
        var roomsResult = await chatFeature.GetUserRoomsAsync(userId);
        return Ok(roomsResult.Dto);
    }

    /// <summary>
    /// Search for room by name
    /// </summary>
    [HttpGet("rooms/search")]
    public async Task<IActionResult> SearchForRoom([FromQuery] string name)
    {
        try
        {
            GetUserId();   
        }catch(UnauthorizedAccessException e)
        {
            return Unauthorized(e.Message);
        }
        
        var roomsResult = await chatFeature.SearchRoomByNameAsync(name);

        return roomsResult.Status switch
        {
            ResultStatus.Success => Ok(roomsResult.Dto),
            ResultStatus.Failure => NotFound("No rooms found"),
            _ => NoContent()
        };
    }

    /// <summary>
    /// Join a chat room.
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/join")]
    public async Task<IActionResult> JoinRoom(Guid roomId)
    {
        try
        {
            var result = await chatFeature.JoinRoomAsync(GetUserId(), roomId);
            return result.Status switch
            {
                ResultStatus.Success => Ok(result),
                ResultStatus.Failure => BadRequest(result),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Leave a chat room.
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/leave")]
    public async Task<IActionResult> LeaveRoom(Guid roomId)
    {
        try
        {
            var result = await chatFeature.LeaveRoomAsync(GetUserId(), roomId);
            return result.Status switch
            {
                ResultStatus.Success => Ok(result),
                ResultStatus.Failure => BadRequest(result),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Get active connection count for a room (admin/debug).
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/connections")]
    public IActionResult GetConnectionCount(Guid roomId)
    {
        throw new NotImplementedException();
    }
    
    public record EditMessageRequest(string NewContent);

    /// <summary> Edit your own message </summary>
    [HttpPatch("messages/{messageId:guid}")]
    public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
    {
        try
        {
            var result = await chatFeature.EditMessageAsync(GetUserId(), messageId, request.NewContent);
            return result.Status switch
            {
                ResultStatus.Success => Ok(result),
                ResultStatus.Unauthorized => Forbid(),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    ///<summary>Soft-delete your own message</summary>
    [HttpDelete("messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid messageId)
    {
        try
        {
            var result = await chatFeature.DeleteMessageAsync(GetUserId(), messageId);
            return result.Status switch
            {
                ResultStatus.Success => Ok(result),
                ResultStatus.Unauthorized => Forbid(),
                _ => BadRequest(result)
            };
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}
