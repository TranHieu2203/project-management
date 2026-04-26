using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;

namespace ProjectManagement.Workforce.Application.Resources.Commands.InactivateResource;

public sealed class InactivateResourceHandler : IRequestHandler<InactivateResourceCommand, ResourceDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public InactivateResourceHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ResourceDto> Handle(InactivateResourceCommand cmd, CancellationToken ct)
    {
        var resource = await _db.Resources
            .Include(r => r.Vendor)
            .FirstOrDefaultAsync(r => r.Id == cmd.ResourceId, ct)
            ?? throw new NotFoundException("Resource không tồn tại.");

        if (resource.Version != cmd.ExpectedVersion)
            throw new ConflictException(
                "Resource đã được cập nhật bởi người khác.",
                CreateResourceHandler.ToDto(resource),
                $"\"{resource.Version}\"");

        resource.Inactivate(cmd.UpdatedBy);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Resource", resource.Id, "Inactivate", cmd.UpdatedBy,
            $"Inactivated resource '{resource.Name}'"), ct);

        return CreateResourceHandler.ToDto(resource);
    }
}
