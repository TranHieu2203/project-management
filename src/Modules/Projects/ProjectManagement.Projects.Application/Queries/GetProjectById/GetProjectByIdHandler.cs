using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Application.Queries.GetProjectById;

public sealed class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectsDbContext _db;

    public GetProjectByIdHandler(IProjectsDbContext db) => _db = db;

    public async Task<ProjectDto> Handle(GetProjectByIdQuery query, CancellationToken ct)
    {
        // CRITICAL: 1 query combining existence + membership check — prevents info leakage
        // Non-member and non-existent project both return 404 (membership-only = no existence confirmation)
        var project = await _db.Projects
            .Where(p => p.Id == query.ProjectId
                     && !p.IsDeleted
                     && p.Members.Any(m => m.UserId == query.CurrentUserId))
            .Select(p => new ProjectDto(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.Status.ToString(),
                p.Visibility,
                p.Version))
            .FirstOrDefaultAsync(ct);

        if (project is null)
            throw new NotFoundException(nameof(Project), query.ProjectId);

        return project;
    }
}
