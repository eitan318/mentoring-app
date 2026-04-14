using System.Net;
using System.Net.Mail;

namespace MentoringApp.Service
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _fromPassword;

        private const int MaxAttempts = 3;

        public EmailService(string smtpHost, int smtpPort, string fromEmail, string fromPassword)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            _fromPassword = fromPassword;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
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
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// SMTP 4xx codes indicate a temporary server-side problem — safe to retry.
        /// </summary>
        private static bool IsTransient(SmtpException ex) =>
            (int)ex.StatusCode >= 400 && (int)ex.StatusCode < 500;
    }
}