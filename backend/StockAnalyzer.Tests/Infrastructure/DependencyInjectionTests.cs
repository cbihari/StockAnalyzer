using Npgsql;
using StockAnalyzer.Infrastructure;

namespace StockAnalyzer.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void BuildConnectionString_ConvertsNeonUrlWithVerifiedSsl()
    {
        var result = DependencyInjection.BuildConnectionString(
            "postgresql://stock%40user:p%3Ass@ep-cool.us-east-2.aws.neon.tech/stockdb?sslmode=require");
        var parsed = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("ep-cool.us-east-2.aws.neon.tech", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("stockdb", parsed.Database);
        Assert.Equal("stock@user", parsed.Username);
        Assert.Equal("p:ss", parsed.Password);
        Assert.Equal(SslMode.VerifyFull, parsed.SslMode);
        Assert.True(parsed.Pooling);
    }

    [Fact]
    public void BuildConnectionString_PreservesNpgsqlFormat()
    {
        const string value = "Host=localhost;Port=5433;Database=stocks;Username=test;Password=test";

        Assert.Equal(value, DependencyInjection.BuildConnectionString(value));
    }

    [Fact]
    public void BuildMlServiceUri_DefaultsOnlyOutsideProduction()
    {
        Assert.Equal(
            new Uri("http://localhost:8000"),
            DependencyInjection.BuildMlServiceUri(null, "Development"));
        Assert.Throws<InvalidOperationException>(() =>
            DependencyInjection.BuildMlServiceUri(null, "Production"));
    }

    [Fact]
    public void BuildMlServiceUri_AddsSchemeForRenderInternalHost()
    {
        Assert.Equal(
            new Uri("http://stockanalyzer-ml:8000"),
            DependencyInjection.BuildMlServiceUri("stockanalyzer-ml:8000", "Production"));
    }
}
