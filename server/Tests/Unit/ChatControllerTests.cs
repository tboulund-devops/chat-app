using System.Security.Claims;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Features;
using Application.Common.Results;
using Application.DTOs.Chat;
using Api.Controllers;
using Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Unit;

public class ChatControllerTests
{
    private readonly IChatFeature _chatFeature = Substitute.For<IChatFeature>();
    private readonly IChatRoomRepository _roomRepository = Substitute.For<IChatRoomRepository>();
    private readonly ISimpleSse _backplane = Substitute.For<ISimpleSse>();
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _controller = new ChatController(_chatFeature, _roomRepository, _backplane)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private void Authenticate(Guid userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test")
        );
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task SendMessage_ShouldReturnOk_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        var request = new SendMessageRequest(Guid.NewGuid(), "Hello world");
        var dto = new ChatMessageDto(request.RoomId, request.RoomId, null, request.Content, DateTime.UtcNow);

        _chatFeature.CreateMessageAsync(userId, request)
            .Returns(Result<ChatMessageDto>.Success(dto));

        var result = await _controller.SendMessage(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, ok.Value);
        await _backplane.Received(1).SendToGroupAsync(request.RoomId, request, "message");
    }

    [Fact]
    public async Task SendMessage_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var request = new SendMessageRequest(Guid.NewGuid(), "Hello");

        var result = await _controller.SendMessage(request);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnOk_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        _chatFeature.GetMessagesAsync(userId, Arg.Any<Guid>(), 0, 50)
            .Returns(Result<IEnumerable<ChatMessageDto>>.Success(Array.Empty<ChatMessageDto>()));

        var result = await _controller.GetMessages(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Result<IEnumerable<ChatMessageDto>>>(ok.Value);
    }

    [Fact]
    public async Task GetMessages_ShouldReturnForbidden_WhenFeatureReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        _chatFeature.GetMessagesAsync(userId, Arg.Any<Guid>(), 0, 50)
            .Returns(Result<IEnumerable<ChatMessageDto>>.Failure("Unauthorized", ResultStatus.Unauthorized));

        var result = await _controller.GetMessages(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateRoom_ShouldReturnCreated_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);

        var request = new CreateRoomRequest("Test Room", "Description");
        var roomDto = new ChatRoomDto { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description, CreatedAt = DateTime.UtcNow };

        _chatFeature.CreateRoomAsync(userId, request)
            .Returns(Result<ChatRoomDto>.Success(roomDto));

        var result = await _controller.CreateRoom(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ChatController.GetMessages), created.ActionName);
        Assert.Same(roomDto, created.Value);
    }

    [Fact]
    public async Task GetAllRooms_ShouldReturnOkWithRoomList()
    {
        var room = Domain.Entities.ChatRoom.Create("Shared", Guid.NewGuid());
        _roomRepository.GetAllRoomsAsync().Returns(Task.FromResult((IEnumerable<Domain.Entities.ChatRoom>)new[] { room }));

        var result = await _controller.GetAllRooms();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(new[] { room }, Assert.IsAssignableFrom<IEnumerable<Domain.Entities.ChatRoom>>(ok.Value));
    }

    [Fact]
    public async Task GetMyRooms_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var result = await _controller.GetMyRooms();
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SearchForRoom_ShouldReturnNotFound_WhenFeatureReturnsFailure()
    {
        Authenticate(Guid.NewGuid());
        _chatFeature.SearchRoomByNameAsync(Arg.Any<string>())
            .Returns(Result<IEnumerable<ChatRoomDto>>.Failure("No room found"));

        var result = await _controller.SearchForRoom("missing");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task JoinRoom_ShouldReturnBadRequest_WhenFeatureReturnsFailure()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);
        _chatFeature.JoinRoomAsync(userId, Arg.Any<Guid>())
            .Returns(Result.Failure("already member", ResultStatus.Failure));

        var result = await _controller.JoinRoom(Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetRoomMembers_ShouldReturnOk_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);
        var members = new[] { new RoomMemberDto { UserId = Guid.NewGuid(), FirstName = "A", LastName = "B", Email = "a@b.com" } };

        _chatFeature.GetRoomMembersAsync(userId, Arg.Any<Guid>())
            .Returns(Result<IEnumerable<RoomMemberDto>>.Success(members));

        var result = await _controller.GetRoomMembers(Guid.NewGuid());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(members, Assert.IsAssignableFrom<IEnumerable<RoomMemberDto>>(ok.Value));
    }

    [Fact]
    public async Task LeaveRoom_ShouldReturnOk_WhenFeatureReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        Authenticate(userId);
        _chatFeature.LeaveRoomAsync(userId, Arg.Any<Guid>())
            .Returns(Result.Success("left"));

        var result = await _controller.LeaveRoom(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetConnectionCount_ShouldThrowNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() => _controller.GetConnectionCount(Guid.NewGuid()));
    }
}
