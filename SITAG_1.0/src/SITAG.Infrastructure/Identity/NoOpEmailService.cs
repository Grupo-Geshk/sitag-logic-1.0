using Microsoft.Extensions.Logging;
using SITAG.Application.Common.Interfaces;

namespace SITAG.Infrastructure.Identity;

/// <summary>
/// Email service that does nothing — logs the intent and returns immediately.
/// Replace with a Resend (or SMTP) implementation when email delivery is needed.
///
/// To wire up Resend:
///   1. Add package: Resend.Net (or use HttpClient with Resend REST API)
///   2. Create ResendEmailService : IEmailService
///   3. Replace the registration in DependencyInjection.cs
/// </summary>
public sealed class NoOpEmailService : IEmailService
{
    private readonly ILogger<NoOpEmailService> _logger;

    public NoOpEmailService(ILogger<NoOpEmailService> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Email no-op] To={To} | Subject={Subject} | (not sent — configure a real email provider)",
            to, subject);
        return Task.CompletedTask;
    }
}
