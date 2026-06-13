using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Api.Auth;

public sealed class JwtTokenService(IConfiguration configuration, IHostEnvironment environment)
{
    public string Create(StockAnalyzerUser user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? configuration["Jwt:Secret"]
            ?? (environment.IsDevelopment()
                ? "local-development-only-change-this-jwt-secret"
                : throw new InvalidOperationException("JWT_SECRET is required outside development."));
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "StockAnalyzer",
            audience: configuration["Jwt:Audience"] ?? "StockAnalyzer.Web",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.DisplayName)
            ],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
