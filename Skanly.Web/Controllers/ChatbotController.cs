// Skanly.Web/Controllers/ChatbotController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Chatbot.DTOs;
using Skanly.Application.Features.Chatbot.Interfaces;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Skanly.Web.Controllers;

[Authorize]
public class ChatbotController : Controller
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Send message (non-streaming, returns JSON) ─────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        SendChatbotMessageDto dto,
        CancellationToken ct)
    {
        if (dto.Stream)
            return BadRequest("Use /Chatbot/Stream for streaming responses.");

        var result = await _chatbotService
            .SendMessageAsync(UserId, dto, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(new { error = result.ErrorMessage });
    }

    // ── Stream response via Server-Sent Events ─────────────────────────────────

    [HttpGet]
    public async Task Stream(
        [FromQuery] int? conversationId,
        [FromQuery] string message,
        [FromQuery] int? currentPropertyId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = 400;
            return;
        }

        // Set SSE headers
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var dto = new SendChatbotMessageDto
        {
            ConversationId = conversationId,
            Message = message,
            CurrentPropertyId = currentPropertyId,
            Stream = true
        };

        var convIdResult = await _chatbotService.StreamMessageAsync(
            UserId,
            dto,
            onToken: async token =>
            {
                var json = JsonSerializer.Serialize(
                    new { type = "token", text = token });
                var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
                await Response.Body.WriteAsync(bytes, ct);
                await Response.Body.FlushAsync(ct);
            },
            ct);

        // Send conversation ID so client can persist it
        var doneJson = JsonSerializer.Serialize(new
        {
            type = "done",
            conversationId = convIdResult.Data
        });
        var doneBytes = Encoding.UTF8.GetBytes($"data: {doneJson}\n\n");
        await Response.Body.WriteAsync(doneBytes, ct);
        await Response.Body.FlushAsync(ct);
    }

    // ── Start new conversation ─────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewConversation(
        int? propertyId,
        CancellationToken ct)
    {
        var result = await _chatbotService
            .StartNewConversationAsync(UserId, propertyId, ct);

        return result.IsSuccess
            ? Ok(new { conversationId = result.Data })
            : BadRequest();
    }

    // ── Load conversation history ──────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Conversation(
        int id,
        CancellationToken ct)
    {
        var result = await _chatbotService
            .GetConversationAsync(UserId, id, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : NotFound();
    }

    // ── Quick suggested prompts context ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Context(
        int? propertyId,
        CancellationToken ct)
    {
        var context = await _chatbotService
            .BuildContextAsync(UserId, propertyId, ct);

        // Return a trimmed version for the UI
        return Ok(new
        {
            hasVerification = context.IsIdentityVerified,
            activeBookings = context.ActiveBookings.Count,
            hasPropertyContext = context.CurrentProperty is not null,
            propertyTitle = context.CurrentProperty?.Title
        });
    }
}