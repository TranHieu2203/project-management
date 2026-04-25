using ProjectManagement.Auth.Domain.Users;

namespace ProjectManagement.Auth.Application.Tokens;

public interface ITokenService
{
    (string Token, int ExpiresInSeconds) CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}

