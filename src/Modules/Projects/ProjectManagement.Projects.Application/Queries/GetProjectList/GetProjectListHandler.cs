using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.DTOs;

namespace ProjectManagement.Projects.Application.Queries.GetProjectList;

public sealed class GetProjectListHandler : IRequestHandler<GetProjectListQuery, List<ProjectDto>>
{
    private readonly IProjectsDbContext _db;

    public GetProjectListHandler(IProjectsDbContext db) => _db = db;

    public async Task<List<ProjectDto>> Handle(GetProjectListQuery query, CancellationToken ct)
    {
        return await _db.Projects
            .Where(p => !p.IsDeleted && p.Members.Any(m => m.UserId == query.CurrentUserId))
            .Select(p => new ProjectDto(
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.Status.ToString(),
                p.Visibility,
                p.Version))
            .ToListAsync(ct);
    }
}
