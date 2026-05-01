using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProjectManagement.Projects.Application.Common.Interfaces;
using ProjectManagement.Projects.Application.IssueTypes.Models;
using ProjectManagement.Projects.Application.IssueTypes.Services;
using ProjectManagement.Projects.Domain.Entities;
using ProjectManagement.Shared.Domain.Exceptions;

namespace ProjectManagement.Projects.Infrastructure.Services;

public sealed class IssueTypesService : IIssueTypesService
{
    private const int MaxNameLength = 50;
    private const int DefaultSortOrder = 0;
    private const string DefaultCustomIconKey = "custom";

    private readonly IProjectsDbContext _db;
    private readonly IMembershipChecker _membershipChecker;

    public IssueTypesService(IProjectsDbContext db, IMembershipChecker membershipChecker)
    {
        _db = db;
        _membershipChecker = membershipChecker;
    }

    public async Task<IReadOnlyList<IssueTypeDto>> GetBuiltInAsync(CancellationToken ct)
    {
        var items = await _db.IssueTypeDefinitions
            .AsNoTracking()
            .Where(x => x.ProjectId == null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(ToDto())
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<IssueTypeDto>> GetByProjectAsync(Guid projectId, Guid currentUserId, CancellationToken ct)
    {
        await _membershipChecker.EnsureMemberAsync(projectId, currentUserId, ct);

        var items = await _db.IssueTypeDefinitions
            .AsNoTracking()
            .Where(x => x.ProjectId == null || x.ProjectId == projectId)
            .OrderBy(x => x.ProjectId == null ? 0 : 1) // built-in first
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(ToDto())
            .ToListAsync(ct);

        return items;
    }

    public async Task<IssueTypeDto> CreateAsync(
        Guid projectId,
        string name,
        string? iconKey,
        string color,
        Guid currentUserId,
        CancellationToken ct)
    {
        await _membershipChecker.EnsureMemberAsync(projectId, currentUserId, ct);
        ValidateName(name);
        ValidateColor(color);

        var trimmedName = name.Trim();
        var normalizedIconKey = string.IsNullOrWhiteSpace(iconKey) ? DefaultCustomIconKey : iconKey.Trim();

        var entity = IssueTypeDefinition.CreateCustom(
            projectId,
            trimmedName,
            normalizedIconKey,
            color,
            DefaultSortOrder,
            currentUserId.ToString());

        _db.IssueTypeDefinitions.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Issue type '{trimmedName}' đã tồn tại trong project.");
        }

        return ToDto(entity);
    }

    public async Task<IssueTypeDto> UpdateAsync(
        Guid projectId,
        Guid typeId,
        string name,
        string? iconKey,
        string color,
        Guid currentUserId,
        CancellationToken ct)
    {
        await _membershipChecker.EnsureMemberAsync(projectId, currentUserId, ct);
        ValidateName(name);
        ValidateColor(color);

        var entity = await _db.IssueTypeDefinitions
            .FirstOrDefaultAsync(x => x.Id == typeId, ct);

        if (entity is null)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        if (entity.ProjectId is null || entity.IsBuiltIn)
            throw new DomainException("Built-in issue type không thể chỉnh sửa.");

        if (entity.ProjectId != projectId)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        var trimmedName = name.Trim();
        var normalizedIconKey = string.IsNullOrWhiteSpace(iconKey) ? DefaultCustomIconKey : iconKey.Trim();
        entity.Update(trimmedName, normalizedIconKey, color, entity.SortOrder, currentUserId.ToString());

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException($"Issue type '{trimmedName}' đã tồn tại trong project.");
        }

        return ToDto(entity);
    }

    public async Task DeleteAsync(Guid projectId, Guid typeId, Guid currentUserId, CancellationToken ct)
    {
        await _membershipChecker.EnsureMemberAsync(projectId, currentUserId, ct);

        var entity = await _db.IssueTypeDefinitions
            .FirstOrDefaultAsync(x => x.Id == typeId, ct);

        if (entity is null)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        if (entity.ProjectId is null || entity.IsBuiltIn || !entity.IsDeletable)
            throw new DomainException("Built-in issue type không thể xóa.");

        if (entity.ProjectId != projectId)
            throw new NotFoundException(nameof(IssueTypeDefinition), typeId);

        var isUsed = await _db.Issues
            .IgnoreQueryFilters()
            .AnyAsync(i => i.IssueTypeId == typeId, ct);

        if (isUsed)
            throw new ConflictException("Issue type đang được dùng bởi ít nhất 1 issue. Không thể xóa.");

        // Soft-delete using AuditableEntity conventions
        entity.SoftDelete(currentUserId.ToString());
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new DomainException("Name là bắt buộc.");
        if (trimmed.Length > MaxNameLength)
            throw new DomainException($"Name tối đa {MaxNameLength} ký tự.");
    }

    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new DomainException("Color là bắt buộc.");

        // Quick validation: "#RRGGBB"
        if (color.Length != 7 || color[0] != '#')
            throw new DomainException("Color phải theo format #RRGGBB.");

        for (var i = 1; i < 7; i++)
        {
            var c = color[i];
            var isHex =
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
            if (!isHex)
                throw new DomainException("Color phải theo format #RRGGBB.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static IssueTypeDto ToDto(IssueTypeDefinition x) =>
        new(x.Id, x.Name, x.IconKey, x.Color, x.IsBuiltIn, x.IsDeletable, x.ProjectId, x.SortOrder);

    private static System.Linq.Expressions.Expression<Func<IssueTypeDefinition, IssueTypeDto>> ToDto()
        => x => new IssueTypeDto(x.Id, x.Name, x.IconKey, x.Color, x.IsBuiltIn, x.IsDeletable, x.ProjectId, x.SortOrder);
}

