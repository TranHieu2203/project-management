using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;

namespace ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorList;

public sealed class GetVendorListHandler : IRequestHandler<GetVendorListQuery, List<VendorDto>>
{
    private readonly IWorkforceDbContext _db;

    public GetVendorListHandler(IWorkforceDbContext db) => _db = db;

    public async Task<List<VendorDto>> Handle(GetVendorListQuery query, CancellationToken ct)
    {
        var q = _db.Vendors.AsNoTracking();

        if (query.ActiveOnly == true)
            q = q.Where(v => v.IsActive);

        var vendors = await q.OrderBy(v => v.Code).ToListAsync(ct);
        return vendors.Select(CreateVendorHandler.ToDto).ToList();
    }
}
