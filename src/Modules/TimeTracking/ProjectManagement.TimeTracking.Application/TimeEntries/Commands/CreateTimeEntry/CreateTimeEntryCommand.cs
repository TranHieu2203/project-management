using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.CreateTimeEntry;

public sealed record CreateTimeEntryCommand(
    Guid ResourceId,
    Guid ProjectId,
    Guid? TaskId,
    DateOnly Date,
    decimal Hours,
    string EntryType,
    string Role,
    string Level,
    string? Note,
    string EnteredBy,
    Guid? SupersedesEntryId = null
) : IRequest<TimeEntryDto>;
