using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Rates.Commands.CreateRate;

namespace ProjectManagement.Workforce.Application.Rates.Queries.GetRateById;

public sealed class GetRateByIdHandler : IRequestHandler<GetRateByIdQuery, MonthlyRateDto>
{
    private readonly IWorkforceDbContext _db;

    public GetRateByIdHandler(IWorkforceDbContext db) => _db = db;

    public async Task<MonthlyRateDto> Handle(GetRateByIdQuery query, CancellationToken ct)
    {
        var rate = await _db.MonthlyRates
            .AsNoTracking()
            .Include(r => r.Vendor)
            .FirstOrDefaultAsync(r => r.Id == query.RateId, ct)
            ?? throw new NotFoundException("Rate không tồn tại.");

        return CreateRateHandler.ToDto(rate);
    }
}
