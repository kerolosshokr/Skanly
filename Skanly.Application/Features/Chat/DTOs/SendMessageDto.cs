// Skanly.Application/Features/Chat/DTOs/SendMessageDto.cs
using Microsoft.AspNetCore.Http;

namespace Skanly.Application.Features.Chat.DTOs;

public class SendMessageDto
{
    public int ConversationId { get; set; }
    public string? MessageText { get; set; }
    public IFormFile? Image { get; set; }
}