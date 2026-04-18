using Application.Common.Interfaces.Services;
using Application.Common.Results;
using Application.DTOs.Chat;
using Application.Features.Chat;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class ChatFeatureTests
{
    private readonly IChatMessageRepository _messageRepository = Substitute.For<IChatMessageRepository>();
    private readonly IChatRoomRepository _roomRepository = Substitute.For<IChatRoomRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IFeatureStateProvider _featureStateProvider = Substitute.For<IFeatureStateProvider>();
    private readonly ChatFeature _chatFeature;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid RoomId = Guid.NewGuid();

    public ChatFeatureTests()
    {
        _featureStateProvider.IsEnabled(Arg.Any<string>()).Returns(true);
        _chatFeature = new ChatFeature(_messageRepository, _roomRepository, _notificationService, _featureStateProvider);
    }

    // ── CreateMessageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateMessageAsync_ShouldReturnFailure_WhenUserIsNotRoomMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(false);
        
       var result = await _chatFeature
           .CreateMessageAsync(
               UserId,
               new SendMessageRequest(
                   RoomId,
                   "Hello"));

        Assert.False(result.IsSuccess);
        Assert.Contains("not a member", result.Message);
    }

    [Fact]
    public async Task CreateMessageAsync_ShouldReturnSuccess_WhenUserIsMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(true);
        _messageRepository.AddAsync(Arg.Any<ChatMessage>())
            .Returns(x => x.Arg<ChatMessage>());

        var result = await _chatFeature
            .CreateMessageAsync(UserId,
                new SendMessageRequest(RoomId, "Hello"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Dto);
        Assert.Equal("Hello", result.Dto!.Content);
    }

    // ── GetMessagesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnUnauthorized_WhenUserIsNotMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(false);

        var result = await _chatFeature.GetMessagesAsync(UserId, RoomId);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task GetMessagesAsync_ShouldReturnMessages_WhenUserIsMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(true);
        _messageRepository.GetByRoomIdAsync(RoomId, 0, 50).Returns([]);

        var result = await _chatFeature.GetMessagesAsync(UserId, RoomId);

        Assert.True(result.IsSuccess);
    }

    // ── CreateRoomAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoomAsync_ShouldReturnSuccess_WithRoomDto()
    {
        _roomRepository.AddAsync(Arg.Any<ChatRoom>())
            .Returns(x => x.Arg<ChatRoom>());
        
       var result = await _chatFeature
           .CreateRoomAsync(UserId,
               new CreateRoomRequest("General", "Room Description"));

        Assert.True(result.IsSuccess);
        Assert.Equal("General", result.Dto!.Name);
    }

    [Fact]
    public async Task CreateRoomAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        _roomRepository.AddAsync(Arg.Any<ChatRoom>())
            .Throws(new RepositoryException("DB error"));

        var result = await _chatFeature.CreateRoomAsync(UserId, new CreateRoomRequest("Boom", "Boom Description"));

        Assert.False(result.IsSuccess);
    }

    // ── JoinRoomAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task JoinRoomAsync_ShouldReturnSuccess_WhenNotAlreadyMember()
    {
        _roomRepository.FindByIdAsync(RoomId).Returns(ChatRoom.Create("Test", UserId, ""));
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(false);

        var result = await _chatFeature.JoinRoomAsync(UserId, RoomId);

        Assert.True(result.IsSuccess);
        await _roomRepository.Received(1).AddMemberAsync(RoomId, UserId);
    }

    [Fact]
    public async Task JoinRoomAsync_ShouldReturnFailure_WhenAlreadyMember()
    {
        _roomRepository.FindByIdAsync(RoomId).Returns(ChatRoom.Create("Test", UserId, ""));
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(true);

        var result = await _chatFeature.JoinRoomAsync(UserId, RoomId);

        Assert.False(result.IsSuccess);
        Assert.Contains("already a member", result.Message);
    }

    [Fact]
    public async Task JoinRoomAsync_ShouldReturnFailure_WhenRoomNotFound()
    {
        var nonExistingRoomId = Guid.NewGuid();
    
        _roomRepository.FindByIdAsync(nonExistingRoomId)
            .Throws(new EntityNotFoundException("Room not found"));

        var result = await _chatFeature.JoinRoomAsync(UserId, nonExistingRoomId);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── LeaveRoomAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task LeaveRoomAsync_ShouldReturnSuccess_WhenUserIsMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(true);

        var result = await _chatFeature.LeaveRoomAsync(UserId, RoomId);

        Assert.True(result.IsSuccess);
        await _roomRepository.Received(1).RemoveMemberAsync(RoomId, UserId);
    }

    [Fact]
    public async Task LeaveRoomAsync_ShouldReturnFailure_WhenUserIsNotMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(false);

        var result = await _chatFeature.LeaveRoomAsync(UserId, RoomId);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a member", result.Message);
    }

    // ── SearchRoomByNameAsync ────────────────────────────────────────────

    [Fact]
    public async Task SearchRoomByNameAsync_ShouldReturnFailure_WhenNoRoomsFound()
    {
        _roomRepository.SearchRoomsByNameAsync(Arg.Any<string>()).Returns([]);

        var result = await _chatFeature.SearchRoomByNameAsync("ghost");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SearchRoomByNameAsync_ShouldReturnRooms_WhenMatchesFound()
    {
        var room = ChatRoom.Create("General", UserId, "");
        _roomRepository.SearchRoomsByNameAsync("Gen").Returns([room]);

        var result = await _chatFeature.SearchRoomByNameAsync("Gen");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Dto!);
    }

    // ── GetUserRoomsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserRoomsAsync_ShouldReturnRooms_WhenUserHasRooms()
    {
        var room = ChatRoom.Create("MyRoom", UserId, "desc");
        _roomRepository.GetRoomsForUserAsync(UserId).Returns([room]);

        var result = await _chatFeature.GetUserRoomsAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Dto!);
    }

    [Fact]
    public async Task GetUserRoomsAsync_ShouldReturnEmptyList_WhenUserHasNoRooms()
    {
        _roomRepository.GetRoomsForUserAsync(UserId).Returns([]);

        var result = await _chatFeature.GetUserRoomsAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Dto!);
    }

    [Fact]
    public async Task GetUserRoomsAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        _roomRepository.GetRoomsForUserAsync(UserId)
            .Throws(new RepositoryException("DB error"));

        var result = await _chatFeature.GetUserRoomsAsync(UserId);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to get rooms", result.Message);
    }

    // ── EditMessageAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task EditMessageAsync_ShouldReturnSuccess_WhenOwnerEditsOwnMessage()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "Original");
        _messageRepository.FindByIdAsync(messageId).Returns(message);
        _messageRepository.UpdateAsync(Arg.Any<ChatMessage>()).Returns(true);

        var result = await _chatFeature.EditMessageAsync(UserId, messageId, "Edited");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EditMessageAsync_ShouldReturnForbidden_WhenNotOwner()
    {
        var messageId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, otherUserId, "Not yours");
        _messageRepository.FindByIdAsync(messageId).Returns(message);

        var result = await _chatFeature.EditMessageAsync(UserId, messageId, "Sneaky edit");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task EditMessageAsync_ShouldReturnFailure_WhenMessageIsDeleted()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "Deleted msg");
        message.IsDeleted = true;
        _messageRepository.FindByIdAsync(messageId).Returns(message);

        var result = await _chatFeature.EditMessageAsync(UserId, messageId, "Revive");

        Assert.False(result.IsSuccess);
        Assert.Contains("deleted", result.Message);
    }

    [Fact]
    public async Task EditMessageAsync_ShouldReturnNotFound_WhenMessageDoesNotExist()
    {
        var messageId = Guid.NewGuid();
        _messageRepository.FindByIdAsync(messageId)
            .Throws(new EntityNotFoundException("Message not found"));

        var result = await _chatFeature.EditMessageAsync(UserId, messageId, "Edit");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task EditMessageAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "Original");
        _messageRepository.FindByIdAsync(messageId).Returns(message);
        _messageRepository.UpdateAsync(Arg.Any<ChatMessage>())
            .Throws(new RepositoryException("DB error"));

        var result = await _chatFeature.EditMessageAsync(UserId, messageId, "Edited");

        Assert.False(result.IsSuccess);
    }

    // ── DeleteMessageAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteMessageAsync_ShouldReturnSuccess_WhenOwnerDeletesOwnMessage()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "To delete");
        _messageRepository.FindByIdAsync(messageId).Returns(message);
        _messageRepository.UpdateAsync(Arg.Any<ChatMessage>()).Returns(true);

        var result = await _chatFeature.DeleteMessageAsync(UserId, messageId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldSoftDelete_BySettingIsDeletedAndReplacingContent()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "Secret message");
        _messageRepository.FindByIdAsync(messageId).Returns(message);
        _messageRepository.UpdateAsync(Arg.Any<ChatMessage>()).Returns(true);

        await _chatFeature.DeleteMessageAsync(UserId, messageId);

        await _messageRepository.Received(1).UpdateAsync(
            Arg.Is<ChatMessage>(m => m.IsDeleted && m.Content == "This message was deleted"));
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldReturnForbidden_WhenNotOwner()
    {
        var messageId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, otherUserId, "Not yours");
        _messageRepository.FindByIdAsync(messageId).Returns(message);

        var result = await _chatFeature.DeleteMessageAsync(UserId, messageId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldReturnNotFound_WhenMessageDoesNotExist()
    {
        var messageId = Guid.NewGuid();
        _messageRepository.FindByIdAsync(messageId)
            .Throws(new EntityNotFoundException("Message not found"));

        var result = await _chatFeature.DeleteMessageAsync(UserId, messageId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteMessageAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        var messageId = Guid.NewGuid();
        var message = ChatMessage.Create(RoomId, UserId, "Msg");
        _messageRepository.FindByIdAsync(messageId).Returns(message);
        _messageRepository.UpdateAsync(Arg.Any<ChatMessage>())
            .Throws(new RepositoryException("DB error"));

        var result = await _chatFeature.DeleteMessageAsync(UserId, messageId);

        Assert.False(result.IsSuccess);
    }

    // ── GetRoomMembersAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRoomMembersAsync_ShouldReturnMembers_WhenUserIsMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(true);
        var member = new ChatRoomMember
        {
            RoomId = RoomId,
            UserId = UserId,
            User = User.Create("Jane", "Doe", "jane@doe.com", [1, 2, 3], RoleType.User)
        };
        _roomRepository.GetMembersAsync(RoomId).Returns([member]);

        var result = await _chatFeature.GetRoomMembersAsync(UserId, RoomId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Dto!);
    }

    [Fact]
    public async Task GetRoomMembersAsync_ShouldReturnUnauthorized_WhenUserIsNotMember()
    {
        _roomRepository.IsMemberAsync(RoomId, UserId).Returns(false);

        var result = await _chatFeature.GetRoomMembersAsync(UserId, RoomId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }
}