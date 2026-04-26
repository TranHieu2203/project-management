using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Application.Vendors.Commands.CreateVendor;

namespace ProjectManagement.Workforce.Application.Vendors.Commands.UpdateVendor;

public sealed class UpdateVendorHandler : IRequestHandler<UpdateVendorCommand, VendorDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public UpdateVendorHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<VendorDto> Handle(UpdateVendorCommand cmd, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == cmd.VendorId, ct)
            ?? throw new NotFoundException("Vendor không tồn tại.");

        if (vendor.Version != cmd.ExpectedVersion)
            throw new ConflictException(
                "Vendor đã được cập nhật bởi người khác.",
                CreateVendorHandler.ToDto(vendor),
                $"\"{vendor.Version}\"");

        vendor.Update(cmd.Name, cmd.Description, cmd.UpdatedBy);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Vendor", vendor.Id, "Update", cmd.UpdatedBy,
            $"Updated vendor '{vendor.Name}'"), ct);

        return CreateVendorHandler.ToDto(vendor);
    }
}
