using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Projects.Domain.Enums;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Commands.CreateProject;

public sealed class CreateProjectHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectsDbContext _db;

    public CreateProjectHandler(IProjectsDbContext db) => _db = db;

    public async Task<ProjectDto> Handle(CreateProjectCommand cmd, CancellationToken ct)
    {
        // Check duplicate code → 409 (không phải 422/400)
        var exists = await _db.Projects.AnyAsync(p => p.Code == cmd.Code && !p.IsDeleted, ct);
        if (exists)
            throw new ConflictException($"Project với code '{cmd.Code}' đã tồn tại.");

        // Tạo project
        var project = Project.Create(cmd.Code, cmd.Name, cmd.Description, cmd.CurrentUserId.ToString());
        _db.Projects.Add(project);

        // Creator tự động thành Manager member — CRITICAL, thiếu thì creator không thấy project vừa tạo
        var membership = ProjectMembership.Create(project.Id, cmd.CurrentUserId, ProjectMemberRole.Manager);
        _db.ProjectMemberships.Add(membership);

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
