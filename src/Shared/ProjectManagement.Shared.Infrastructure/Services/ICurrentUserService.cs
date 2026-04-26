namespace ProjectManagement.Shared.Infrastructure.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
