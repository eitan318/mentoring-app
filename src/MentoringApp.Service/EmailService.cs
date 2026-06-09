using System.Net;
using System.Net.Mail;

namespace MentoringApp.Service
{
    /// <summary>
    /// Low-level SMTP email sender. Retries up to <see cref="MaxAttempts"/> times on failure.
    /// SMTP credentials are injected from configuration; higher-level NotificationService composes the messages.
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        private const int MaxAttempts = 3;

        /// <summary>The reason the last send failed (null on success). Surfaced so callers can show a useful message.</summary>
        public string? LastError { get; private set; }

        public EmailService(string smtpHost, int smtpPort, string fromEmail, string fromPassword)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            // Gmail (and most providers) display app passwords with spaces for readability,
            // but the actual credential has none — strip whitespace or SMTP auth fails with 535.
            _fromPassword = (fromPassword ?? string.Empty).Replace(" ", string.Empty);
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            LastError = null;

            // Fail fast with a clear message if the sender isn't configured.
            if (string.IsNullOrWhiteSpace(_smtpHost) ||
                string.IsNullOrWhiteSpace(_fromEmail) ||
                string.IsNullOrWhiteSpace(_fromPassword))
            {
                LastError = "Email sender is not configured (SmtpHost / FromEmail / FromPassword missing).";
                Console.Error.WriteLine($"[EmailService] {LastError}");
                return false;
            }

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    using var client = new SmtpClient(_smtpHost, _smtpPort)
                    {
                        Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                        EnableSsl = true
                    };

                    var message = new MailMessage(_fromEmail, to, subject, htmlBody) { IsBodyHtml = true };
                    await client.SendMailAsync(message);
                    return true;
                }
                catch (SmtpException ex) when (IsTransient(ex) && attempt < MaxAttempts)
                {
                    LastError = ex.Message;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s
                }
                catch (Exception ex)
                {
                    // Surface the real cause (auth failure, blocked port, etc.) instead of swallowing it.
                    LastError = ex.Message;
                    Console.Error.WriteLine($"[EmailService] Send to '{to}' failed: {ex}");
                    return false;
                }
            }

            LastError ??= "Email sending failed after multiple attempts.";
            Console.Error.WriteLine($"[EmailService] {LastError}");
            return false;
        }

        /// <summary>
        /// SMTP 4xx codes indicate a temporary server-side problem — safe to retry.
        /// </summary>
        private static bool IsTransient(SmtpException ex) =>
            (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500;
    }
}