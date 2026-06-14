using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<StockAnalyzerUser> userManager,
    JwtTokenService tokenService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponseDto>> Signup(SignupRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();
        if (displayName.Length is < 2 or > 80)
            return BadRequest(new ProblemDetails { Detail = "Display name must be between 2 and 80 characters." });

        var user = new StockAnalyzerUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName
        };
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new ProblemDetails { Detail = string.Join(" ", result.Errors.Select(error => error.Description)) });
        return Ok(BuildResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new ProblemDetails { Detail = "Email or password is incorrect." });
        return Ok(BuildResponse(user));
    }

    [HttpGet("google")]
    public IActionResult Google() => Challenge(
        new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback)) },
        GoogleDefaults.AuthenticationScheme);

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        var providerKey = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(providerKey))
            return Unauthorized();

        var user = await userManager.FindByLoginAsync(GoogleDefaults.AuthenticationScheme, providerKey)
            ?? await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new StockAnalyzerUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = result.Principal?.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0]
            };
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) return BadRequest();
        }
        if ((await userManager.GetLoginsAsync(user)).All(login =>
                login.LoginProvider != GoogleDefaults.AuthenticationScheme || login.ProviderKey != providerKey))
        {
            var login = await userManager.AddLoginAsync(user, new UserLoginInfo(
                GoogleDefaults.AuthenticationScheme,
                providerKey,
                "Google"));
            if (!login.Succeeded) return BadRequest();
        }

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        var frontendUrl = (Environment.GetEnvironmentVariable("FRONTEND_URL")
            ?? configuration["FrontendUrl"]
            ?? "http://localhost:4200").TrimEnd('/');
        return Redirect($"{frontendUrl}/auth/callback#token={Uri.EscapeDataString(tokenService.Create(user))}");
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(ToDto(user));
    }

    private AuthResponseDto BuildResponse(StockAnalyzerUser user) => new(tokenService.Create(user), ToDto(user));
    private static AuthUserDto ToDto(StockAnalyzerUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.CreatedAt);
}
