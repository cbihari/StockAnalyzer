using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/monetization")]
public sealed class MonetizationController(
    IMonetizationService monetizationService,
    IMonetizationEventRepository eventRepository,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Returns plan metadata and current usage for the browser workspace or signed-in user.</summary>
    [HttpGet("status")]
    [ProducesResponseType<MonetizationStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonetizationStatusDto>> GetStatus(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await monetizationService.GetStatusAsync(
            User.GetUserId(),
            clientId,
            cancellationToken));

    /// <summary>Checks whether a monetized feature is available before recording usage.</summary>
    [HttpGet("usage/check")]
    [ProducesResponseType<UsageCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageCheckDto>> CheckUsage(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromQuery] string featureKey,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default) =>
        Ok(await monetizationService.CheckAsync(
            User.GetUserId(),
            clientId,
            featureKey,
            quantity,
            cancellationToken));

    /// <summary>Records usage for a monetized feature when the current plan has quota.</summary>
    [HttpPost("usage")]
    [ProducesResponseType<UsageCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageCheckDto>> RecordUsage(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] RecordUsageRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await monetizationService.RecordAsync(
            User.GetUserId(),
            clientId,
            request.FeatureKey,
            request.Quantity,
            cancellationToken));

    /// <summary>Records a bounded first-party monetization analytics event.</summary>
    [HttpPost("events")]
    [ProducesResponseType<MonetizationEventResponseDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonetizationEventResponseDto>> RecordEvent(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] MonetizationEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await monetizationService.RecordEventAsync(
            User.GetUserId(),
            clientId,
            request,
            cancellationToken);
        return Accepted(result);
    }

    /// <summary>Returns aggregate monetization funnel events for admins.</summary>
    [Authorize(Policy = AuthPolicies.AffiliateAdmin)]
    [HttpGet("events/funnel")]
    [ProducesResponseType<MonetizationFunnelReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MonetizationFunnelReportDto>> GetFunnel(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!AdminReportsEnabled())
        {
            return NotFound();
        }

        if (!AuthPolicies.IsAffiliateAdmin(User))
        {
            return Forbid();
        }

        var (from, to) = ResolveFunnelRange(days);
        return Ok(await eventRepository.GetFunnelAsync(from, to, cancellationToken));
    }

    /// <summary>Exports aggregate monetization funnel events as CSV for admins.</summary>
    [Authorize(Policy = AuthPolicies.AffiliateAdmin)]
    [HttpGet("events/funnel/export")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFunnel(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!AdminReportsEnabled())
        {
            return NotFound();
        }

        if (!AuthPolicies.IsAffiliateAdmin(User))
        {
            return Forbid();
        }

        var (from, to) = ResolveFunnelRange(days);
        var report = await eventRepository.GetFunnelAsync(from, to, cancellationToken);
        return File(
            Encoding.UTF8.GetBytes(BuildFunnelCsv(report)),
            "text/csv;charset=utf-8",
            $"stockanalyzer-monetization-funnel-{from:yyyy-MM-dd}-to-{to:yyyy-MM-dd}.csv");
    }

    /// <summary>Creates a checkout session for a paid plan. Access changes after provider confirmation.</summary>
    [Authorize]
    [HttpPost("checkout")]
    [ProducesResponseType<CheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckoutResponseDto>> StartCheckout(
        [FromBody] CheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails { Detail = "Sign in before starting checkout." });
        }

        return Ok(await monetizationService.StartCheckoutAsync(
            userId.Value,
            request,
            cancellationToken));
    }

    private bool AdminReportsEnabled() =>
        bool.TryParse(
            Environment.GetEnvironmentVariable("MONETIZATION_ADMIN_ENABLED")
                ?? configuration["MonetizationAdminEnabled"]
                ?? Environment.GetEnvironmentVariable("AFFILIATE_ADMIN_ENABLED")
                ?? configuration["AffiliateAdminEnabled"],
            out var value) && value;

    private static (DateOnly From, DateOnly To) ResolveFunnelRange(int days)
    {
        var clampedDays = Math.Clamp(days, 1, 90);
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        return (to.AddDays(-clampedDays + 1), to);
    }

    private static string BuildFunnelCsv(MonetizationFunnelReportDto report)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, ["section", "event_name", "source", "feature_key", "plan_key", "count", "from", "to"]);
        foreach (var item in report.Events)
        {
            AppendCsvRow(builder,
            [
                "event_total",
                item.EventName,
                string.Empty,
                string.Empty,
                string.Empty,
                item.Count.ToString(CultureInfo.InvariantCulture),
                report.From.ToString("O", CultureInfo.InvariantCulture),
                report.To.ToString("O", CultureInfo.InvariantCulture)
            ]);
        }

        foreach (var item in report.Breakdown)
        {
            AppendCsvRow(builder,
            [
                "breakdown",
                item.EventName,
                item.Source,
                item.FeatureKey ?? string.Empty,
                item.PlanKey ?? string.Empty,
                item.Count.ToString(CultureInfo.InvariantCulture),
                report.From.ToString("O", CultureInfo.InvariantCulture),
                report.To.ToString("O", CultureInfo.InvariantCulture)
            ]);
        }

        foreach (var item in report.Daily)
        {
            AppendCsvRow(builder,
            [
                "daily",
                item.EventName,
                string.Empty,
                string.Empty,
                string.Empty,
                item.Count.ToString(CultureInfo.InvariantCulture),
                item.Date.ToString("O", CultureInfo.InvariantCulture),
                item.Date.ToString("O", CultureInfo.InvariantCulture)
            ]);
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        builder.AppendJoin(',', values.Select(EscapeCsv));
        builder.Append('\n');
    }

    private static string EscapeCsv(string value) =>
        string.Concat('"', value.Replace("\"", "\"\"", StringComparison.Ordinal), '"');

    /// <summary>Accepts payment provider webhooks and updates subscription state after verification.</summary>
    [HttpPost("webhooks/{provider}")]
    [ProducesResponseType<PaymentWebhookResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentWebhookResultDto>> HandleWebhook(
        string provider,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        return Ok(await monetizationService.HandleWebhookAsync(
            provider,
            payload,
            headers,
            cancellationToken));
    }
}
