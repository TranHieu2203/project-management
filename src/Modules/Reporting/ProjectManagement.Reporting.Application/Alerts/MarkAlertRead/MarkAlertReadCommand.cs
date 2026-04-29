using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;

namespace ProjectManagement.Reporting.Application.Alerts.MarkAlertRead;

public sealed record MarkAlertReadCommand(Guid AlertId, Guid CurrentUserId) : IRequest;

public sealed class MarkAlertReadHandler : IRequestHandler<MarkAlertReadCommand>
{
    private readonly IReportingDbContext _db;

    public MarkAlertReadHandler(IReportingDbContext db) => _db = db;

    public async Task Handle(MarkAlertReadCommand request, CancellationToken ct)
    {
        var alert = await _db.Alerts
            .FirstOrDefaultAsync(a => a.Id == request.AlertId, ct)
            ?? throw new KeyNotFoundException($"Alert {request.AlertId} not found.");

        if (alert.UserId != request.CurrentUserId)
            throw new UnauthorizedAccessException("Cannot mark another user's alert as read.");

        if (!alert.IsRead)
        {
            alert.MarkAsRead();
            await _db.SaveChangesAsync(ct);
        }
    }
}
