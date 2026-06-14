using Microsoft.AspNetCore.Identity;

namespace StockAnalyzer.Infrastructure.Identity;

public sealed class StockAnalyzerUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
