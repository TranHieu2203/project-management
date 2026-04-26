namespace ProjectManagement.TimeTracking.Application.Common.Interfaces;

public interface ITimeTrackingRateService
{
    Task<decimal> GetHourlyRateAsync(
        Guid resourceId,
        string role,
        string level,
        DateOnly date,
        CancellationToken ct);
}
