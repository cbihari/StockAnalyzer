namespace StockAnalyzer.Application.DTOs;

public sealed record SignupRequestDto(string Email, string Password, string DisplayName);
public sealed record LoginRequestDto(string Email, string Password);
public sealed record AuthUserDto(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);
public sealed record AuthResponseDto(string Token, AuthUserDto User);
public sealed record AuthConfigDto(bool GoogleEnabled);
public sealed record ClaimWorkspaceRequestDto(string WorkspaceId);
