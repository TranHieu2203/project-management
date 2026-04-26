using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Domain.Entities;

namespace ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;

public sealed class CreateVendorHandler : IRequestHandler<CreateVendorCommand, VendorDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public CreateVendorHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<VendorDto> Handle(CreateVendorCommand cmd, CancellationToken ct)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Code == cmd.Code, ct);
        if (exists)
            throw new ConflictException($"Vendor với code '{cmd.Code}' đã tồn tại.");

        var vendor = Vendor.Create(cmd.Code, cmd.Name, cmd.Description, cmd.CreatedBy);
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Vendor", vendor.Id, "Create", cmd.CreatedBy,
            $"Created vendor '{vendor.Name}' (code: {vendor.Code})"), ct);

        return ToDto(vendor);
    }

    internal static VendorDto ToDto(Vendor v) => new(
        v.Id, v.Code, v.Name, v.Description, v.IsActive, v.Version,
        v.CreatedAt, v.CreatedBy, v.UpdatedAt, v.UpdatedBy);
}
