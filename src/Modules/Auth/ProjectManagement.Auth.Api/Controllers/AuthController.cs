using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Auth.Application.Models;
using ProjectManagement.Auth.Application.Tokens;
using ProjectManagement.Auth.Domain.Users;

namespace ProjectManagement.Auth.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            return UnauthorizedProblem();
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return UnauthorizedProblem();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresInSeconds) = _tokenService.CreateAccessToken(user, roles.ToArray());

        var dto = new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName);
        return Ok(new LoginResponse(token, "Bearer", expiresInSeconds, dto));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }

    private UnauthorizedObjectResult UnauthorizedProblem()
    {
        return Unauthorized(new ProblemDetails
        {
            Title = "Không đăng nhập được",
            Detail = "Tên đăng nhập hoặc mật khẩu không đúng. Vui lòng kiểm tra và thử lại.",
            Status = StatusCodes.Status401Unauthorized,
            Type = "Unauthorized"
        });
    }
}

