using MediatR;
using ProjectManagement.TimeTracking.Application.DTOs;

namespace ProjectManagement.TimeTracking.Application.TimeEntries.Commands.VoidTimeEntry;

public sealed record VoidTimeEntryCommand(
    Guid EntryId,
    string Reason,
    string VoidedBy
) : IRequest<TimeEntryDto>;
