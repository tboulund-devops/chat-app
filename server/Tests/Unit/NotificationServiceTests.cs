using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using NSubstitute;

namespace Unit;

public class NotificationServiceTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IChatRoomRepository _roomRepository = Substitute.For<IChatRoomRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISimpleSse _backplane = Substitute.For<ISimpleSse>();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(
            _notificationRepository,
            _roomRepository,
            _userRepository,
            _backplane);
    }

    [Fact]
    public async Task NotifyPokeAsync_ShouldCreateNotificationAndSendEvent()
    {
        var pokerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var poker = User.Create("Poke", "Master", "poker@example.com", new byte[] { 1, 2, 3 });

        _userRepository.FindByIdAsync(pokerId).Returns(poker);
        _notificationRepository.AddAsync(Arg.Any<Notification>())
            .Returns(call => Task.FromResult(call.Arg<Notification>()));

        await _service.NotifyPokeAsync(pokerId, targetUserId);

        await _notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n =>
            n.RecipientId == targetUserId &&
            n.Type == NotificationType.Poke &&
            n.Payload.Contains(pokerId.ToString())));

        await _backplane.Received(1).SendToUserAsync(targetUserId, Arg.Any<object>(), "notification");
    }

    [Fact]
    public async Task NotifyNewMessageAsync_ShouldNotifyAllExceptSender()
    {
        var senderId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var sender = User.Create("Sender", "One", "sender@example.com", new byte[] { 1, 2, 3 });
        var room = ChatRoom.Create("Room", Guid.NewGuid());
        room = room with { Name = "Room" };
        var otherUser = User.Create("Target", "User", "target@example.com", new byte[] { 4, 5, 6 });
        var member = ChatRoomMember.Create(roomId, otherUser.Id) with { User = otherUser, Room = room };

        _roomRepository.GetMembersAsync(roomId).Returns(new[] { member, ChatRoomMember.Create(roomId, senderId) with { User = sender, Room = room } });
        _roomRepository.FindByIdAsync(roomId).Returns(room);
        _userRepository.FindByIdAsync(senderId).Returns(sender);
        _notificationRepository.AddAsync(Arg.Any<Notification>())
            .Returns(call => Task.FromResult(call.Arg<Notification>()));

        await _service.NotifyNewMessageAsync(senderId, roomId, messageId, "Hello");

        await _notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n =>
            n.RecipientId == otherUser.Id &&
            n.Type == NotificationType.NewMessage &&
            n.Payload.Contains("Hello")));

        await _backplane.Received(1).SendToUserAsync(otherUser.Id, Arg.Any<object>(), "notification");
    }
}
