
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

        public EmailService(string smtpHost, int smtpPort, string fromEmail, string fromPassword)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _fromEmail = fromEmail;
            _fromPassword = fromPassword;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
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
            catch { return false; }
        }
    }
}