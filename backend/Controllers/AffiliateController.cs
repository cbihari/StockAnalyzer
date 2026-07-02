using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/affiliate")]
public sealed class AffiliateController(
    IAffiliateClickRepository repository,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("partners")]
    public ActionResult<IReadOnlyList<AffiliatePartnerDto>> GetPartners() => Ok(LoadPartners());

    [HttpPost("click")]
    public async Task<IActionResult> RecordClick(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] AffiliateClickRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(clientId, out _))
            return BadRequest(new ProblemDetails { Detail = "A valid anonymous workspace ID is required." });

        var partner = LoadPartners().FirstOrDefault(item =>
            string.Equals(item.Name, request.Broker?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (partner is null)
            return BadRequest(new ProblemDetails { Detail = "The broker is not configured." });

        await repository.AddAsync(new AffiliateClick
        {
            Broker = partner.Name,
            Ticker = NormalizeTicker(request.Ticker),
            ClientId = clientId,
            ClickedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        return Accepted();
    }

    [Authorize(Policy = AuthPolicies.AffiliateAdmin)]
    [HttpGet("stats")]
    public async Task<ActionResult<IReadOnlyList<AffiliateClickStatDto>>> GetStats(
        CancellationToken cancellationToken)
    {
        var enabled = bool.TryParse(
            Environment.GetEnvironmentVariable("AFFILIATE_ADMIN_ENABLED")
                ?? configuration["AffiliateAdminEnabled"],
            out var value) && value;
        if (!enabled) return NotFound();
        if (!AuthPolicies.IsAffiliateAdmin(User)) return Forbid();
        return Ok(await repository.GetStatsAsync(cancellationToken));
    }

    private IReadOnlyList<AffiliatePartnerDto> LoadPartners()
    {
        var json = Environment.GetEnvironmentVariable("AFFILIATE_LINKS_CONFIG");
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                return JsonSerializer.Deserialize<List<AffiliatePartnerDto>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return configuration.GetSection("AffiliatePartners").Get<List<AffiliatePartnerDto>>() ?? [];
    }

    private static string? NormalizeTicker(string? ticker)
    {
        var value = ticker?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 80)];
    }
}
