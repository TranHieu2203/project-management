using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Auth.Domain.Users;
using ProjectManagement.Shared.Infrastructure.Services;

namespace ProjectManagement.Auth.Infrastructure.Services;

public sealed class UserLookupService : IUserLookupService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserLookupService(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public async Task<IReadOnlyList<UserBasicDto>> GetUsersByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.ToHashSet();
        return await _userManager.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserBasicDto(u.Id, u.UserName!, u.DisplayName))
            .ToListAsync(ct);
    }
}
