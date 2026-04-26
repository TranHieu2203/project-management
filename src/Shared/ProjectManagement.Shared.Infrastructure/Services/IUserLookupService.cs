namespace ProjectManagement.Shared.Infrastructure.Services;

public sealed record UserBasicDto(Guid Id, string Username, string? DisplayName);

public interface IUserLookupService
{
    Task<IReadOnlyList<UserBasicDto>> GetUsersByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
}
