using Microsoft.AspNetCore.Identity;

namespace ProjectManagement.Auth.Domain.Users;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}

