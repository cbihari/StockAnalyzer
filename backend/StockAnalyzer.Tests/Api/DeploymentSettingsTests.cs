using StockAnalyzer.Api.Configuration;

namespace StockAnalyzer.Tests.Api;

public sealed class DeploymentSettingsTests
{
    [Fact]
    public void ResolveAllowedOrigins_TrimsAndDeduplicatesOrigins()
    {
        var origins = DeploymentSettings.ResolveAllowedOrigins(
            " https://app.example.com,https://app.example.com, http://localhost:4200 ",
            isDevelopment: false);

        Assert.Equal(["https://app.example.com", "http://localhost:4200"], origins);
    }

    [Fact]
    public void ResolveAllowedOrigins_RequiresConfigurationInProduction()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DeploymentSettings.ResolveAllowedOrigins(null, isDevelopment: false));
    }

    [Fact]
    public void ResolveAllowedOrigins_RejectsNonHttpOrigins()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DeploymentSettings.ResolveAllowedOrigins("file:///tmp/app", isDevelopment: false));
    }
}
