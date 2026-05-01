using Microsoft.EntityFrameworkCore;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.IssueTypes.Models;
using ProjectManagement.Projects.Application.IssueTypes.Services;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Infrastructure.Services;

public sealed class ProjectIssueTypeSettingsService : IProjectIssueTypeSettingsService
{
    private readonly IProjectsDbContext _db;
    private readonly IProjectAdminChecker _projectAdminChecker;

    public ProjectIssueTypeSettingsService(IProjectsDbContext db, IProjectAdminChecker projectAdminChecker)
    {
        _db = db;
        _projectAdminChecker = projectAdminChecker;
    }

    public async Task<IReadOnlyList<IssueTypeSettingDto>> GetAsync(Guid projectId, Guid currentUserId, CancellationToken ct)
    {
        await _projectAdminChecker.EnsureProjectAdminAsync(projectId, currentUserId, ct);

        var typesQuery = _db.IssueTypeDefinitions
            .AsNoTracking()
            .Where(x => x.ProjectId == null || x.ProjectId == projectId);

        var settingsQuery = _db.ProjectIssueTypeSettings
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId);

        var items = await (
            from t in typesQuery
            join s in settingsQuery on t.Id equals s.IssueTypeId into gj
            from s in gj.DefaultIfEmpty()
            select new IssueTypeSettingDto(
                t.Id,
                t.Name,
                t.IconKey,
                t.Color,
                t.IsBuiltIn,
                t.IsDeletable,
                t.ProjectId,
                t.SortOrder,
                s == null ? true : s.IsEnabled
            ))
            .OrderBy(x => x.ProjectId == null ? 0 : 1) // built-in first
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return items;
    }

    public async Task<IssueTypeSettingDto> SetEnabledAsync(
        Guid projectId,
        Guid typeId,
        bool isEnabled,
        Guid currentUserId,
        CancellationToken ct)
    {
        await _projectAdminChecker.EnsureProjectAdminAsync(projectId, currentUserId, ct);

        var type = await _db.IssueTypeDefinitions
            .FirstOrDefaultAsync(x => x.Id == typeId, ct);

        if (type is null)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        if (type.ProjectId is not null && type.ProjectId != projectId)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        var setting = await _db.ProjectIssueTypeSettings
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.IssueTypeId == typeId, ct);

        if (setting is null)
        {
            setting = ProjectIssueTypeSetting.Create(projectId, typeId, isEnabled, currentUserId.ToString());
            _db.ProjectIssueTypeSettings.Add(setting);
        }
        else
        {
            setting.SetEnabled(isEnabled, currentUserId.ToString());
        }

        await _db.SaveChangesAsync(ct);

        return new IssueTypeSettingDto(
            type.Id,
            type.Name,
            type.IconKey,
            type.Color,
            type.IsBuiltIn,
            type.IsDeletable,
            type.ProjectId,
            type.SortOrder,
            isEnabled);
    }
}

