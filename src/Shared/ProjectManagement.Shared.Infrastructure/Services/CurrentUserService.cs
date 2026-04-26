using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ProjectManagement.Shared.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            // JWT Bearer middleware maps "sub" → ClaimTypes.NameIdentifier by default
            var value = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? _accessor.HttpContext?.User.FindFirstValue("sub");

            if (Guid.TryParse(value, out var id))
                return id;

            throw new InvalidOperationException(
                "Cannot resolve UserId: user is not authenticated or sub claim is missing.");
        }
    }

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
