using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;

namespace ProjectManagement.Workforce.Application.Vendors.Queries.GetVendorById;

public sealed class GetVendorByIdHandler : IRequestHandler<GetVendorByIdQuery, VendorDto>
{
    private readonly IWorkforceDbContext _db;

    public GetVendorByIdHandler(IWorkforceDbContext db) => _db = db;

    public async Task<VendorDto> Handle(GetVendorByIdQuery query, CancellationToken ct)
    {
        var vendor = await _db.Vendors.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == query.VendorId, ct)
            ?? throw new NotFoundException("Vendor không tồn tại.");

        return CreateVendorHandler.ToDto(vendor);
    }
}
