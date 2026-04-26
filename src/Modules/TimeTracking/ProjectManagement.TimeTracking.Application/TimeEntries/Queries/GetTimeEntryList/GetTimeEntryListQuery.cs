using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryList;

public sealed record GetTimeEntryListQuery(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    Guid? ResourceId = null,
    Guid? ProjectId = null,
    string? EntryType = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<TimeEntryDto>>;
