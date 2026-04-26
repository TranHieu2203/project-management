using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Commands.UpdateProject;

public sealed class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membershipChecker;

    public UpdateProjectHandler(IProjectsDbContext db, IMembershipChecker membershipChecker)
    {
        _db = db;
        _membershipChecker = membershipChecker;
    }

    public async Task<ProjectDto> Handle(UpdateProjectCommand cmd, CancellationToken ct)
    {
        // EnsureMember → NotFoundException (→ 404) nếu không phải member
        await _membershipChecker.EnsureMemberAsync(cmd.ProjectId, cmd.CurrentUserId, ct);

        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == cmd.ProjectId && !p.IsDeleted, ct);
        if (project is null)
            throw new NotFoundException(nameof(Project), cmd.ProjectId);

        // Version mismatch → 409 với CurrentState + CurrentETag
        if (project.Version != cmd.ExpectedVersion)
        {
            var currentDto = new ProjectDto(
                project.Id,
                project.Code,
                project.Name,
                project.Description,
                project.Status.ToString(),
                project.Visibility,
                project.Version);

            throw new ConflictException(
                "Project đã được chỉnh sửa bởi người khác. Vui lòng tải lại.",
                currentState: currentDto,
                currentETag: $"\"{project.Version}\"");
        }

        project.Update(cmd.Name, cmd.Description, cmd.CurrentUserId.ToString());
        await _db.SaveChangesAsync(ct);

        return new ProjectDto(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            project.Status.ToString(),
            project.Visibility,
            project.Version);
    }
}
