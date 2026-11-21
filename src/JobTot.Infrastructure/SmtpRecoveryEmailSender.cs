using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using JobTot.Application.Authentication;
namespace JobTot.Infrastructure;

public sealed class SmtpRecoveryEmailSender(IConfiguration configuration) : IRecoveryEmailSender
{
    public async Task SendAsync(string email, string link, CancellationToken ct)
    {
        try
        {
            var settings = configuration.GetSection("PasswordRecovery:Smtp");
            var host =
                settings["Host"]
                ?? throw new InvalidOperationException("Password recovery SMTP is not configured.");
            var from =
                settings["From"]
                ?? throw new InvalidOperationException(
                    "Password recovery sender is not configured."
                );
            using var client = new SmtpClient(host, settings.GetValue("Port", 587))
            {
                EnableSsl = settings.GetValue("EnableSsl", true),
                UseDefaultCredentials = false,
            };
            if (!string.IsNullOrWhiteSpace(settings["Username"]))
                client.Credentials = new NetworkCredential(
                    settings["Username"],
                    settings["Password"]
                );
            using var message = new MailMessage(from, email)
            {
                Subject = "Khôi phục mật khẩu Job Tốt",
                Body =
                    $"Bạn đã yêu cầu đặt lại mật khẩu Job Tốt. Mở liên kết sau trong 30 phút:\n\n{link}\n\nNếu không gửi yêu cầu này, bạn có thể bỏ qua email.",
            };
            await client.SendMailAsync(message, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            global::JobTot.Application.ExceptionDiagnostics.Record(
                exception,
                "SmtpRecoveryEmailSender.SendAsync"
            );
            throw;
        }
    }
}

