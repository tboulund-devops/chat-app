using Application.Common.Interfaces.Services;
using Application.Common.Results;
using Application.Features.Notifications;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Unit;

public class NotificationFeatureTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly NotificationFeature _feature;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid NotificationId = Guid.NewGuid();

    public NotificationFeatureTests()
    {
        _feature = new NotificationFeature(
            _notificationRepository,
            _userRepository,
            _notificationService);
    }

    // ── PokeUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task PokeUserAsync_ShouldReturnSuccess_WhenTargetExists()
    {
        _userRepository.FindByIdAsync(TargetId)
            .Returns(User.Create("Jane", "Doe", "jane@doe.com", [], RoleType.User));

        var result = await _feature.PokeUserAsync(UserId, TargetId);

        Assert.True(result.IsSuccess);
        await _notificationService.Received(1).NotifyPokeAsync(UserId, TargetId);
    }

    [Fact]
    public async Task PokeUserAsync_ShouldReturnNotFound_WhenTargetDoesNotExist()
    {
        _userRepository.FindByIdAsync(TargetId)
            .Throws(new EntityNotFoundException("User not found"));

        var result = await _feature.PokeUserAsync(UserId, TargetId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _notificationService.DidNotReceive().NotifyPokeAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    // ── GetUnreadAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadAsync_ShouldReturnEmptyList_WhenNoNotifications()
    {
        _notificationRepository.GetByUserIdAsync(UserId, unreadOnly: true).Returns([]);

        var result = await _feature.GetUnreadAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Dto!);
    }

    [Fact]
    public async Task GetUnreadAsync_ShouldReturnOnlyUnread_WhenNotificationsExist()
    {
        var notification = Notification.Create(UserId, NotificationType.Poke, "{}");
        _notificationRepository.GetByUserIdAsync(UserId, unreadOnly: true)
            .Returns([notification]);

        var result = await _feature.GetUnreadAsync(UserId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Dto!);
    }

    // ── MarkReadAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task MarkReadAsync_ShouldReturnSuccess_WhenOwnerMarksOwnNotification()
    {
        var notification = Notification.Create(UserId, NotificationType.Poke, "{}");
        _notificationRepository.FindByIdAsync(notification.Id).Returns(notification);

        var result = await _feature.MarkReadAsync(UserId, notification.Id);

        Assert.True(result.IsSuccess);
        await _notificationRepository.Received(1).MarkAsReadAsync(notification.Id);
    }

    [Fact]
    public async Task MarkReadAsync_ShouldReturnUnauthorized_WhenNotOwner()
    {
        var otherUserId = Guid.NewGuid();
        var notification = Notification.Create(otherUserId, NotificationType.Poke, "{}");
        _notificationRepository.FindByIdAsync(notification.Id).Returns(notification);

        var result = await _feature.MarkReadAsync(UserId, notification.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        await _notificationRepository.DidNotReceive().MarkAsReadAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task MarkReadAsync_ShouldReturnNotFound_WhenNotificationDoesNotExist()
    {
        _notificationRepository.FindByIdAsync(NotificationId)
            .Throws(new EntityNotFoundException("Notification not found"));

        var result = await _feature.MarkReadAsync(UserId, NotificationId);

        Assert.False(result.IsSuccess);
    }

    // ── MarkAllReadAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllReadAsync_ShouldReturnSuccess_Always()
    {
        var result = await _feature.MarkAllReadAsync(UserId);

        Assert.True(result.IsSuccess);
        await _notificationRepository.Received(1).MarkAllAsReadAsync(UserId);
    }
}