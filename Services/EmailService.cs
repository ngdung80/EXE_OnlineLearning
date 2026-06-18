using MailKit.Net.Smtp;
using MimeKit;

namespace POT_System_ASPNET.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendVerificationCodeAsync(string toEmail, string code);
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = _config.GetSection("EmailSettings");
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings["SenderName"] ?? "", settings["SenderEmail"] ?? ""));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings["SmtpHost"] ?? "", int.Parse(settings["SmtpPort"] ?? "25"), false);
        await client.AuthenticateAsync(settings["SenderEmail"] ?? "", settings["SenderPassword"] ?? "");
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var html = $@"
        <div style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;padding:30px;background:#1e293b;color:#fff;border-radius:12px;'>
            <h2 style='color:#ec6090;'>Personalized Learning Online</h2>
            <p>Your verification code is:</p>
            <div style='font-size:36px;font-weight:700;color:#ec6090;letter-spacing:8px;text-align:center;padding:20px;background:#2d3748;border-radius:8px;'>{code}</div>
            <p style='color:#94a3b8;margin-top:20px;'>This code expires in 5 minutes.</p>
        </div>";
        await SendEmailAsync(toEmail, "PLO – Email Verification Code", html);
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var html = $@"
        <div style='font-family:Inter,sans-serif;max-width:600px;margin:0 auto;padding:30px;background:#1e293b;color:#fff;border-radius:12px;'>
            <h2 style='color:#ec6090;'>Password Reset</h2>
            <p>Click the button below to reset your password:</p>
            <a href='{resetLink}' style='display:inline-block;padding:14px 28px;background:linear-gradient(135deg,#ec6090,#e75485);color:#fff;border-radius:25px;text-decoration:none;font-weight:600;'>Reset Password</a>
            <p style='color:#94a3b8;margin-top:20px;'>If you didn't request this, please ignore this email.</p>
        </div>";
        await SendEmailAsync(toEmail, "PLO – Password Reset", html);
    }
}
