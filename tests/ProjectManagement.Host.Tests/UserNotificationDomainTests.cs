using ProjectManagement.Notifications.Domain.Entities;
using ProjectManagement.Notifications.Domain.Enums;

namespace ProjectManagement.Host.Tests;

public sealed class UserNotificationDomainTests
{
    [Fact]
    public void Create_SetsIsReadFalseAndCreatedAtPopulated()
    {
        var before = DateTime.UtcNow;
        var recipientId = Guid.NewGuid();

        var n = UserNotification.Create(
            recipientUserId: recipientId,
            type: NotificationType.Assigned,
            title: "Test title",
            body: "Test body",
            entityType: "task",
            entityId: Guid.NewGuid());

        Assert.False(n.IsRead);
        Assert.Null(n.ReadAt);
        Assert.NotEqual(Guid.Empty, n.Id);
        Assert.Equal(recipientId, n.RecipientUserId);
        Assert.Equal(NotificationType.Assigned, n.Type);
        Assert.Equal("Test title", n.Title);
        Assert.Equal("Test body", n.Body);
        Assert.Equal("task", n.EntityType);
        Assert.True(n.CreatedAt >= before);
    }

    [Fact]
    public void Create_WithoutEntityInfo_NullEntityFieldsAllowed()
    {
        var n = UserNotification.Create(Guid.NewGuid(), NotificationType.StatusChanged, "t", "b");

        Assert.Null(n.EntityType);
        Assert.Null(n.EntityId);
    }

    [Fact]
    public void MarkRead_SetsIsReadTrueAndReadAtPopulated()
    {
        var n = UserNotification.Create(Guid.NewGuid(), NotificationType.Assigned, "t", "b");
        var before = DateTime.UtcNow;

        n.MarkRead();

        Assert.True(n.IsRead);
        Assert.NotNull(n.ReadAt);
        Assert.True(n.ReadAt >= before);
    }

    [Fact]
    public void MarkRead_CalledTwice_ReadAtUpdated()
    {
        var n = UserNotification.Create(Guid.NewGuid(), NotificationType.Assigned, "t", "b");
        n.MarkRead();
        var firstReadAt = n.ReadAt;

        n.MarkRead();

        Assert.True(n.IsRead);
        Assert.True(n.ReadAt >= firstReadAt);
    }
}
