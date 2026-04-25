namespace ProjectManagement.Auth.Application.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Email, string? DisplayName);

public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresInSeconds, UserDto User);

