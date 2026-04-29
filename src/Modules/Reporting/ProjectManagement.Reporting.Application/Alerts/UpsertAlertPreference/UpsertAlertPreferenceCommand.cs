using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Reporting.Application.Common.Interfaces;
using ProjectManagement.Reporting.Domain.Entities;

namespace ProjectManagement.Reporting.Application.Alerts.UpsertAlertPreference;

public sealed record UpsertAlertPreferenceCommand(
    Guid CurrentUserId,
    string AlertType,
    bool Enabled,
    int? ThresholdDays) : IRequest;

public sealed class UpsertAlertPreferenceHandler : IRequestHandler<UpsertAlertPreferenceCommand>
{
    private readonly IReportingDbContext _db;

    public UpsertAlertPreferenceHandler(IReportingDbContext db) => _db = db;

    public async Task Handle(UpsertAlertPreferenceCommand request, CancellationToken ct)
    {
        var existing = await _db.AlertPreferences
            .FirstOrDefaultAsync(p =>
                p.UserId == request.CurrentUserId &&
                p.AlertType == request.AlertType, ct);

        if (existing is not null)
        {
            existing.Update(request.Enabled, request.ThresholdDays);
        }
        else
        {
            var preference = AlertPreference.Create(
                request.CurrentUserId,
                request.AlertType,
                request.Enabled,
                request.ThresholdDays);
            _db.AlertPreferences.Add(preference);
        }

        await _db.SaveChangesAsync(ct);
    }
}
