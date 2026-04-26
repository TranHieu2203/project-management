using MediatR;
using ProjectManagement.Workforce.Application.DTOs;

namespace ProjectManagement.Workforce.Application.Rates.Queries.GetRateList;

public sealed record GetRateListQuery(
    Guid? VendorId = null,
    int? Year = null,
    int? Month = null
) : IRequest<List<MonthlyRateDto>>;
