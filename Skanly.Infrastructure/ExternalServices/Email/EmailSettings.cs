// Skanly.Infrastructure/ExternalServices/Email/EmailSettings.cs
namespace Skanly.Infrastructure.ExternalServices.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}