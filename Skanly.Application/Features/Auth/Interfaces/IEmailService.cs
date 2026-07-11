// Skanly.Application/Features/Auth/Interfaces/IEmailService.cs
namespace Skanly.Application.Features.Auth.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken ct = default);

    Task SendEmailConfirmationAsync(
        string toEmail,
        string userName,
        string confirmationLink,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail,
        string userName,
        string resetLink,
        CancellationToken ct = default);

    Task SendBookingConfirmationAsync(
        string toEmail,
        string userName,
        string propertyTitle,
        string bookingDetails,
        CancellationToken ct = default);

    Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken ct = default);
}