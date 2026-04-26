using MediatR;
using ProjectManagement.TimeTracking.Application.ImportJobs.DTOs;

namespace ProjectManagement.TimeTracking.Application.ImportJobs.Commands.StartImportJob;

public sealed record CsvColumnMapping(
    string ResourceIdColumn,
    string ProjectIdColumn,
    string DateColumn,
    string HoursColumn,
    string RoleColumn,
    string LevelColumn,
    string? NoteColumn,
    string? TaskIdColumn
);

public sealed record StartImportJobCommand(
    Guid VendorId,
    string FileName,
    string RawCsvContent,
    CsvColumnMapping ColumnMapping,
    string EnteredBy
) : IRequest<ImportJobDto>;
