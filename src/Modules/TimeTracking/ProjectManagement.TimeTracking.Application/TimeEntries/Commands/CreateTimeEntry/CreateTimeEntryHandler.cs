using MediatR;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.TimeTracking.Application.DTOs;
using ProjectManagement.TimeTracking.Domain.Entities;
using ProjectManagement.TimeTracking.Domain.Enums;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;

public sealed class CreateTimeEntryHandler : IRequestHandler<CreateTimeEntryCommand, TimeEntryDto>
{
    private readonly ITimeTrackingDbContext _db;
    private readonly ITimeTrackingRateService _rateService;

    public CreateTimeEntryHandler(ITimeTrackingDbContext db, ITimeTrackingRateService rateService)
    {
        _db = db;
        _rateService = rateService;
    }

    public async Task<TimeEntryDto> Handle(CreateTimeEntryCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<TimeEntryStatus>(cmd.EntryType, out var status))
            throw new DomainException($"EntryType không hợp lệ: '{cmd.EntryType}'. Chấp nhận: Estimated, PmAdjusted.");

        if (status == TimeEntryStatus.VendorConfirmed)
            throw new DomainException("EntryType 'VendorConfirmed' chỉ được set qua vendor import pipeline.");

        if (cmd.Hours <= 0)
            throw new DomainException("Hours phải lớn hơn 0.");

        if (cmd.Hours > 24)
            throw new DomainException("Hours không thể vượt quá 24h/ngày.");

        if (cmd.SupersedesEntryId.HasValue)
        {
            var original = await _db.TimeEntries.FindAsync([cmd.SupersedesEntryId.Value], ct)
                ?? throw new NotFoundException($"Entry gốc {cmd.SupersedesEntryId.Value} không tồn tại.");
            _ = original; // validates existence
            if (string.IsNullOrWhiteSpace(cmd.Note))
                throw new DomainException("Correction entry bắt buộc phải có Note (reason).");
        }

        var hourlyRate = await _rateService.GetHourlyRateAsync(
            cmd.ResourceId, cmd.Role, cmd.Level, cmd.Date, ct);

        var entry = TimeEntry.Create(
            cmd.ResourceId,
            cmd.ProjectId,
            cmd.TaskId,
            cmd.Date,
            cmd.Hours,
            cmd.EntryType,
            cmd.Note,
            hourlyRate,
            cmd.EnteredBy,
            cmd.SupersedesEntryId);

        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        return ToDto(entry);
    }

    internal static TimeEntryDto ToDto(TimeEntry e) => new(
        e.Id, e.ResourceId, e.ProjectId, e.TaskId,
        e.Date, e.Hours, e.EntryType, e.Note,
        e.RateAtTime, e.CostAtTime, e.EnteredBy, e.CreatedAt,
        e.IsVoided, e.VoidReason, e.VoidedBy, e.VoidedAt, e.SupersedesId);
}
