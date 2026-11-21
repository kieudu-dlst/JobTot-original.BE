using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using JobTot.Application.Authentication;
namespace JobTot.Infrastructure;

public sealed class SmtpAccountEmailSender(IConfiguration configuration) : IAccountEmailSender
{
    public async Task SendAsync(string email, string subject, AccountEmailContent body, CancellationToken ct)
    {
        var settings = configuration.GetSection("PasswordRecovery:Smtp");
        if (
            string.IsNullOrWhiteSpace(settings["Host"])
            || string.IsNullOrWhiteSpace(settings["From"])
        )
            throw new EmailSecurityException(
                503,
                "Chưa cấu hình gửi email. Vui lòng liên hệ quản trị viên."
            );
        using var smtp = new SmtpClient(settings["Host"], settings.GetValue("Port", 587))
        {
            EnableSsl = settings.GetValue("EnableSsl", true),
            UseDefaultCredentials = false,
        };
        if (!string.IsNullOrWhiteSpace(settings["Username"]))
            smtp.Credentials = new NetworkCredential(settings["Username"], settings["Password"]);
        using var mail = new MailMessage(settings["From"]!, email)
        {
            Subject = subject,
            Body = body.Text,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
        };
        mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            body.Html, Encoding.UTF8, "text/html"));
        try
        {
            await smtp.SendMailAsync(mail, ct);
        }
        catch (SmtpException)
        {
            throw new EmailSecurityException(503, "Chưa gửi được email. Vui lòng thử lại sau.");
        }
    }
}

