using MediatR;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Infrastructure.Notifications;

public sealed class WorkforceMutatedEventHandler
    : INotificationHandler<WorkforceMutatedNotification>
{
    private readonly IWorkforceDbContext _db;

    public WorkforceMutatedEventHandler(IWorkforceDbContext db) => _db = db;

    public async Task Handle(WorkforceMutatedNotification notification, CancellationToken ct)
    {
        var auditEvent = AuditEvent.Create(
            notification.EntityType,
            notification.EntityId,
            notification.Action,
            notification.Actor,
            notification.Summary);
        _db.AuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync(ct);
    }
}
