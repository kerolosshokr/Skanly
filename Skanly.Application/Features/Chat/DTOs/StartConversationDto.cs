// Skanly.Application/Features/Chat/DTOs/StartConversationDto.cs
namespace Skanly.Application.Features.Chat.DTOs;

public class StartConversationDto
{
    public string OwnerId { get; set; } = string.Empty;
    public int? PropertyId { get; set; }
}