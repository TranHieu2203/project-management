namespace ProjectManagement.Reporting.Application.Queries.GetProjectsSummary;

public sealed record ProjectSummaryDto(
    Guid ProjectId,
    string Name,
    string HealthStatus,          // OnTrack | AtRisk | Delayed
    decimal PercentComplete,
    decimal PercentTimeElapsed,
    int RemainingTaskCount,
    int OverdueTaskCount
);
