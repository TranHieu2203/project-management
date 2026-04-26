using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Application.Resources.Queries.GetResourceList;

public sealed class GetResourceListHandler : IRequestHandler<GetResourceListQuery, List<ResourceDto>>
{
    private readonly IWorkforceDbContext _db;

    public GetResourceListHandler(IWorkforceDbContext db) => _db = db;

    public async Task<List<ResourceDto>> Handle(GetResourceListQuery query, CancellationToken ct)
    {
        var q = _db.Resources.AsNoTracking().Include(r => r.Vendor).AsQueryable();

        if (!string.IsNullOrEmpty(query.Type) && Enum.TryParse<ResourceType>(query.Type, out var type))
            q = q.Where(r => r.Type == type);

        if (query.VendorId.HasValue)
            q = q.Where(r => r.VendorId == query.VendorId.Value);

        if (query.ActiveOnly == true)
            q = q.Where(r => r.IsActive);

        var resources = await q.OrderBy(r => r.Code).ToListAsync(ct);
        return resources.Select(r => CreateResourceHandler.ToDto(r)).ToList();
    }
}
