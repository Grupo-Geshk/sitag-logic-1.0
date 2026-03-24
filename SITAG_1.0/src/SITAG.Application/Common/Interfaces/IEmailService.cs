namespace SITAG.Application.Common.Interfaces;

/// <summary>
/// Abstraction over transactional email delivery.
/// The no-op implementation is registered by default; swap it for a
/// Resend (or any other provider) implementation when ready.
/// </summary>
public interface IEmailService
{
    /// <summary>Send a transactional email.</summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">Full HTML body of the message.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
