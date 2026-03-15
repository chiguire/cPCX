using System.Net;
using System.Net.Mail;
using cpcx.Config;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace cpcx.Services;

public class SmtpEmailSender(IOptions<SmtpConfig> config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var cfg = config.Value;
        if (string.IsNullOrEmpty(cfg.Host))
        {
            logger.LogWarning("SMTP host not configured — skipping email to {Email}", email);
            return;
        }
        using var client = new SmtpClient(cfg.Host, cfg.Port)
        {
            Credentials = new NetworkCredential(cfg.Username, cfg.Password),
            EnableSsl = cfg.EnableSsl,
        };
        using var message = new MailMessage(
            new MailAddress(cfg.FromAddress, cfg.FromName),
            new MailAddress(email))
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };
        await client.SendMailAsync(message);
    }
}
