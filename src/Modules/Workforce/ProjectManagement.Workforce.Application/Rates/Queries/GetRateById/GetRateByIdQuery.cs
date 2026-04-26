using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Rates.Queries.GetRateById;

public sealed record GetRateByIdQuery(Guid RateId) : IRequest<MonthlyRateDto>;
