using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Domain.Entities;
using ProjectManagement.Workforce.Domain.Enums;

namespace ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;

public sealed class CreateResourceHandler : IRequestHandler<CreateResourceCommand, ResourceDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public CreateResourceHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ResourceDto> Handle(CreateResourceCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<ResourceType>(cmd.Type, out var resourceType))
            throw new DomainException($"Loại resource không hợp lệ: '{cmd.Type}'. Chỉ chấp nhận 'Inhouse' hoặc 'Outsource'.");

        if (resourceType == ResourceType.Outsource && cmd.VendorId is null)
            throw new DomainException("Resource Outsource bắt buộc phải có vendorId.");

        if (resourceType == ResourceType.Inhouse && cmd.VendorId is not null)
            throw new DomainException("Resource Inhouse không được có vendorId.");

        var codeExists = await _db.Resources.AnyAsync(r => r.Code == cmd.Code, ct);
        if (codeExists)
            throw new ConflictException($"Resource với code '{cmd.Code}' đã tồn tại.");

        Vendor? vendor = null;
        if (cmd.VendorId.HasValue)
        {
            vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == cmd.VendorId.Value, ct);
            if (vendor is null || !vendor.IsActive)
                throw new DomainException($"Vendor '{cmd.VendorId}' không tồn tại hoặc không active.");
        }

        var resource = Resource.Create(cmd.Code, cmd.Name, cmd.Email, resourceType, cmd.VendorId, cmd.CreatedBy);
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Resource", resource.Id, "Create", cmd.CreatedBy,
            $"Created resource '{resource.Name}' (code: {resource.Code}, type: {resource.Type})"), ct);

        return ToDto(resource, vendor?.Name);
    }

    internal static ResourceDto ToDto(Resource r, string? vendorName = null) => new(
        r.Id, r.Code, r.Name, r.Email, r.Type.ToString(),
        r.VendorId, vendorName ?? r.Vendor?.Name,
        r.IsActive, r.Version,
        r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy);
}
