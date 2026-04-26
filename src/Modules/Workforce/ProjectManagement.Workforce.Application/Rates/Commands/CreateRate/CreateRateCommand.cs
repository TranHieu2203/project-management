using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Rates.Commands.CreateRate;

public sealed record CreateRateCommand(
    Guid VendorId,
    string Role,
    string Level,
    int Year,
    int Month,
    decimal MonthlyAmount,
    string CreatedBy
) : IRequest<MonthlyRateDto>;
