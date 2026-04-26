using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;

namespace ProjectManagement.Workforce.Application.Resources.Queries.GetResourceById;

public sealed class GetResourceByIdHandler : IRequestHandler<GetResourceByIdQuery, ResourceDto>
{
    private readonly IWorkforceDbContext _db;

    public GetResourceByIdHandler(IWorkforceDbContext db) => _db = db;

    public async Task<ResourceDto> Handle(GetResourceByIdQuery query, CancellationToken ct)
    {
        var resource = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Vendor)
            .FirstOrDefaultAsync(r => r.Id == query.ResourceId, ct)
            ?? throw new NotFoundException("Resource không tồn tại.");

        return CreateResourceHandler.ToDto(resource);
    }
}
