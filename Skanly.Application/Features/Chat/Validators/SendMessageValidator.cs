// Skanly.Application/Features/Chat/Validators/SendMessageValidator.cs
using FluentValidation;
using Skanly.Application.Features.Chat.DTOs;

namespace Skanly.Application.Features.Chat.Validators;

public class SendMessageValidator : AbstractValidator<SendMessageDto>
{
    private static readonly string[] AllowedImageTypes =
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0)
            .WithMessage("Invalid conversation.");

        // Either text or image must be present — not both empty
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.MessageText) || x.Image != null)
            .WithMessage("Message must contain text or an image.");

        RuleFor(x => x.MessageText)
            .MaximumLength(1000)
            .WithMessage("Message cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.MessageText));

        When(x => x.Image != null, () =>
        {
            RuleFor(x => x.Image!.Length)
                .LessThanOrEqualTo(MaxImageBytes)
                .WithMessage("Image must not exceed 5 MB.");

            RuleFor(x => x.Image!.FileName)
                .Must(name => AllowedImageTypes.Contains(
                    Path.GetExtension(name).ToLowerInvariant()))
                .WithMessage("Only JPG, PNG, GIF, and WEBP images are accepted.");
        });
    }
}