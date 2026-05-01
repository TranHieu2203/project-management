using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Notifications.Application.Common.Interfaces;
using ProjectManagement.Notifications.Domain.Entities;
using ProjectManagement.Notifications.Domain.Enums;
using ProjectManagement.Projects.Application.Notifications;

namespace ProjectManagement.Notifications.Application.EventHandlers;

public sealed class PerEventNotificationHandler :
    INotificationHandler<TaskAssignedNotification>,
    INotificationHandler<TaskStatusChangedNotification>
{
    private readonly INotificationsDbContext _db;
    private readonly IEmailService _emailSvc;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PerEventNotificationHandler> _logger;

    public PerEventNotificationHandler(
        INotificationsDbContext db,
        IEmailService emailSvc,
        UserManager<ApplicationUser> userManager,
        ILogger<PerEventNotificationHandler> logger)
    {
        _db = db;
        _emailSvc = emailSvc;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task Handle(TaskAssignedNotification n, CancellationToken ct)
    {
        try
        {
            if (!n.NewAssigneeUserId.HasValue) return;

            var user = await _userManager.FindByIdAsync(n.NewAssigneeUserId.Value.ToString());
            if (user is null) return;

            var pref = await _db.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == NotificationType.Assigned, ct);
            if (pref?.IsEnabled == false) return;

            var title = $"Task '{n.TaskName}' được giao cho bạn";
            var body  = $"Dự án: {n.ProjectName}. Vui lòng kiểm tra và xử lý.";

            _db.UserNotifications.Add(UserNotification.Create(
                recipientUserId: user.Id,
                type: NotificationType.Assigned,
                title: title,
                body: body,
                entityType: "task",
                entityId: n.TaskId,
                projectId: n.ProjectId));
            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrEmpty(user.Email))
            {
                var html = $"<h3>{title}</h3><p>{body}</p>"
                         + $"<p><a href='/projects/{n.ProjectId}/tasks/{n.TaskId}'>Xem task →</a></p>";
                await _emailSvc.SendAsync(user.Email, $"[PM Tool] {title}", html, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PerEventNotificationHandler: error handling TaskAssignedNotification TaskId={TaskId}",
                n.TaskId);
        }
    }

    public async Task Handle(TaskStatusChangedNotification n, CancellationToken ct)
    {
        try
        {
            if (!n.AssigneeUserId.HasValue) return;

            var user = await _userManager.FindByIdAsync(n.AssigneeUserId.Value.ToString());
            if (user is null) return;

            var pref = await _db.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.Type == NotificationType.StatusChanged, ct);
            if (pref?.IsEnabled == false) return;

            var title = $"Task '{n.TaskName}' chuyển sang trạng thái '{n.NewStatus}'";
            var body  = $"Dự án: {n.ProjectName}. Trạng thái cũ: {n.PreviousStatus}.";

            _db.UserNotifications.Add(UserNotification.Create(
                recipientUserId: user.Id,
                type: NotificationType.StatusChanged,
                title: title,
                body: body,
                entityType: "task",
                entityId: n.TaskId,
                projectId: n.ProjectId));
            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrEmpty(user.Email))
            {
                var html = $"<h3>{title}</h3><p>{body}</p>"
                         + $"<p><a href='/projects/{n.ProjectId}/tasks/{n.TaskId}'>Xem task →</a></p>";
                await _emailSvc.SendAsync(user.Email, $"[PM Tool] {title}", html, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PerEventNotificationHandler: error handling TaskStatusChangedNotification TaskId={TaskId}",
                n.TaskId);
        }
    }
}
