using System.Security.Claims;

namespace StockAnalyzer.Api.Auth;

public static class AuthPolicies
{
    public const string AffiliateAdmin = "AffiliateAdmin";
    public const string AdminRole = "Admin";
    public const string AdminClaimType = "stockanalyzer_admin";

    public static bool IsAffiliateAdmin(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true &&
        (user.IsInRole(AdminRole) || user.HasClaim(AdminClaimType, "true"));
}
