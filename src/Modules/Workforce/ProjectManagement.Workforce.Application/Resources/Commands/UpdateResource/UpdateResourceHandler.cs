using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Domain.Exceptions;
using ProjectManagement.Workforce.Application.Common.Interfaces;
using ProjectManagement.Workforce.Application.DTOs;
using ProjectManagement.Workforce.Application.Notifications;
using ProjectManagement.Workforce.Application.Resources.Commands.CreateResource;

namespace ProjectManagement.Workforce.Application.Resources.Commands.UpdateResource;

public sealed class UpdateResourceHandler : IRequestHandler<UpdateResourceCommand, ResourceDto>
{
    private readonly IWorkforceDbContext _db;
    private readonly IMediator _mediator;

    public UpdateResourceHandler(IWorkforceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<ResourceDto> Handle(UpdateResourceCommand cmd, CancellationToken ct)
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

        resource.Update(cmd.Name, cmd.Email, cmd.UpdatedBy);
        await _db.SaveChangesAsync(ct);

        await _mediator.Publish(new WorkforceMutatedNotification(
            "Resource", resource.Id, "Update", cmd.UpdatedBy,
            $"Updated resource '{resource.Name}'"), ct);

        return CreateResourceHandler.ToDto(resource);
    }
}
