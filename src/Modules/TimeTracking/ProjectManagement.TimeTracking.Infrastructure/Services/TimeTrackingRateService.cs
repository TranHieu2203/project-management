using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.TimeTracking.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.Common.Interfaces;

namespace ProjectManagement.TimeTracking.Infrastructure.Services;

public sealed class TimeTrackingRateService : ITimeTrackingRateService
{
    private readonly IWorkforceDbContext _workforce;

    public TimeTrackingRateService(IWorkforceDbContext workforce) => _workforce = workforce;

    public async Task<decimal> GetHourlyRateAsync(
        Guid resourceId,
        string role,
        string level,
        DateOnly date,
        CancellationToken ct)
    {
        var resource = await _workforce.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == resourceId, ct)
            ?? throw new DomainException($"Resource '{resourceId}' không tồn tại.");

        if (!resource.IsActive)
            throw new DomainException($"Resource '{resource.Name}' đã inactive.");

        if (resource.VendorId is null)
            return 0m;

        var rate = await _workforce.MonthlyRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.VendorId == resource.VendorId.Value &&
                r.Role == role &&
                r.Level == level &&
                r.Year == date.Year &&
                r.Month == date.Month, ct);

        if (rate is null)
            throw new DomainException(
                $"Không tìm thấy rate cho role '{role}'/level '{level}' tháng {date.Month}/{date.Year}.");

        return rate.HourlyRate;
    }
}
