// Skanly.Infrastructure/ExternalServices/Email/EmailService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skanly.Application.Features.Auth.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Skanly.Infrastructure.ExternalServices.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent to {Email} | Subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // Don't throw — email failure should never break the main flow
        }
    }

    public async Task SendEmailConfirmationAsync(
        string toEmail,
        string userName,
        string confirmationLink,
        CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
              <div style="background:#6C63FF;padding:30px;border-radius:8px 8px 0 0;text-align:center;">
                <h1 style="color:white;margin:0;">Skanly</h1>
                <p style="color:#E0E0FF;margin:5px 0 0;">Smart Student Housing</p>
              </div>
              <div style="background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;border:1px solid #eee;">
                <h2 style="color:#333;">Confirm Your Email</h2>
                <p style="color:#666;">Hi {userName},</p>
                <p style="color:#666;">Welcome to Skanly! Please confirm your email address to activate your account.</p>
                <div style="text-align:center;margin:30px 0;">
                  <a href="{confirmationLink}"
                     style="background:#6C63FF;color:white;padding:14px 30px;
                            border-radius:6px;text-decoration:none;font-weight:bold;
                            display:inline-block;">
                    Confirm Email
                  </a>
                </div>
                <p style="color:#999;font-size:13px;">
                  This link expires in 24 hours. If you did not create an account, you can safely ignore this email.
                </p>
              </div>
            </div>
            """;

        await SendEmailAsync(toEmail, "Confirm your Skanly account", html, ct);
    }

    public async Task SendPasswordResetAsync(
        string toEmail,
        string userName,
        string resetLink,
        CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
              <div style="background:#6C63FF;padding:30px;border-radius:8px 8px 0 0;text-align:center;">
                <h1 style="color:white;margin:0;">Skanly</h1>
              </div>
              <div style="background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;border:1px solid #eee;">
                <h2 style="color:#333;">Reset Your Password</h2>
                <p style="color:#666;">Hi {userName},</p>
                <p style="color:#666;">We received a request to reset your Skanly password.</p>
                <div style="text-align:center;margin:30px 0;">
                  <a href="{resetLink}"
                     style="background:#FF6B6B;color:white;padding:14px 30px;
                            border-radius:6px;text-decoration:none;font-weight:bold;
                            display:inline-block;">
                    Reset Password
                  </a>
                </div>
                <p style="color:#999;font-size:13px;">
                  This link expires in 1 hour. If you did not request a password reset, please ignore this email.
                </p>
              </div>
            </div>
            """;

        await SendEmailAsync(toEmail, "Reset your Skanly password", html, ct);
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken ct = default)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
              <div style="background:#6C63FF;padding:30px;border-radius:8px 8px 0 0;text-align:center;">
                <h1 style="color:white;margin:0;">Welcome to Skanly!</h1>
              </div>
              <div style="background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;border:1px solid #eee;">
                <p style="color:#666;">Hi {userName}, your account is now active.</p>
                <p style="color:#666;">Start exploring verified student housing near your university today.</p>
              </div>
            </div>
            """;

        await SendEmailAsync(toEmail, "Welcome to Skanly!", html, ct);
    }

    public async Task SendBookingConfirmationAsync(
     string toEmail,
     string userName,
     string propertyTitle,
     string bookingDetails,
     CancellationToken ct = default)
    {
        var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
          <div style="background:#6C63FF;padding:30px;border-radius:8px 8px 0 0;text-align:center;">
            <h1 style="color:white;margin:0;">Booking Confirmed!</h1>
          </div>

          <div style="background:#f9f9f9;padding:30px;border-radius:0 0 8px 8px;border:1px solid #eee;">
            <p>Hi {userName}, your booking for <strong>{propertyTitle}</strong> has been confirmed.</p>

            <pre style="background:#fff;padding:15px;border-radius:4px;border:1px solid #eee;">
              {bookingDetails}
            </pre>
          </div>
        </div>
        """;

        await SendEmailAsync(
            toEmail,
            $"Booking Confirmed – {propertyTitle}",
            html,
            ct);
    }
}
    

