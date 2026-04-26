using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Queries.GetTimeEntryById;

public sealed record GetTimeEntryByIdQuery(Guid EntryId) : IRequest<TimeEntryDto>;
