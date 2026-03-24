using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Identity;

/// <summary>
/// Email delivery via the Resend REST API (https://resend.com).
///
/// Required configuration:
///   RESEND_API_KEY  — API key from the Resend dashboard
///   EMAIL_FROM      — Verified sender address, e.g. "SITAG <noreply@sitag.app>"
///
/// Resend requires a verified sending domain — you cannot send from a Gmail
/// address. Verify your domain at https://resend.com/domains and then set
/// EMAIL_FROM to an address on that domain.
/// </summary>
public sealed class ResendEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly string     _from;
    private readonly ILogger<ResendEmailService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ResendEmailService(
        HttpClient              http,
        IConfiguration          config,
        ILogger<ResendEmailService> logger)
    {
        _http   = http;
        _logger = logger;
        _from   = config["EMAIL_FROM"] ?? "SITAG <noreply@sitag.app>";

        var apiKey = config["RESEND_API_KEY"]
            ?? throw new InvalidOperationException("RESEND_API_KEY is not configured.");

        _http.BaseAddress = new Uri("https://api.resend.com/");
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var payload = new { from = _from, to = new[] { to }, subject, html = htmlBody };
        var json    = JsonSerializer.Serialize(payload, _jsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("emails", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Resend delivery failed: {Status} — {Error}", (int)response.StatusCode, error);
            throw new InvalidOperationException($"Email delivery failed ({(int)response.StatusCode}): {error}");
        }

        _logger.LogInformation("Email sent via Resend: To={To} Subject={Subject}", to, subject);
    }
}
