namespace StockAnalyzer.Api.Configuration;

internal static class DeploymentSettings
{
    internal static string[] ResolveAllowedOrigins(string? value, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("ALLOWED_ORIGINS is required outside development.");
            }
            value = "http://localhost:4200";
        }

        var origins = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (origins.Length == 0 || origins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new InvalidOperationException("ALLOWED_ORIGINS must contain valid HTTP or HTTPS origins.");
        }
        return origins;
    }
}
