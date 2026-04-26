using MediatR;
using ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;
using ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;

namespace ProjectManagement.TimeTracking.Application.ImportJobs.Commands.ApplyImportJob;

public sealed record ApplyImportJobCommand(
    Guid JobId,
    string EnteredBy,
    CsvColumnMapping ColumnMapping
) : IRequest<ImportJobDto>;
