using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Rates.Commands.CreateRate;

namespace ProjectManagement.Workforce.Application.Rates.Queries.GetRateList;

public sealed class GetRateListHandler : IRequestHandler<GetRateListQuery, List<MonthlyRateDto>>
{
    private readonly IWorkforceDbContext _db;

    public GetRateListHandler(IWorkforceDbContext db) => _db = db;

    public async Task<List<MonthlyRateDto>> Handle(GetRateListQuery query, CancellationToken ct)
    {
        var q = _db.MonthlyRates.AsNoTracking().Include(r => r.Vendor).AsQueryable();

        if (query.VendorId.HasValue) q = q.Where(r => r.VendorId == query.VendorId.Value);
        if (query.Year.HasValue)     q = q.Where(r => r.Year == query.Year.Value);
        if (query.Month.HasValue)    q = q.Where(r => r.Month == query.Month.Value);

        var rates = await q
            .OrderBy(r => r.Year).ThenBy(r => r.Month)
            .ThenBy(r => r.Role).ThenBy(r => r.Level)
            .ToListAsync(ct);

        return rates.Select(r => CreateRateHandler.ToDto(r)).ToList();
    }
}
