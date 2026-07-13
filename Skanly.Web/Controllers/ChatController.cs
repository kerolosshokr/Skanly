// Skanly.Web/Controllers/ChatController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skanly.Application.Features.Chat.DTOs;
using Skanly.Application.Features.Chat.Interfaces;
using System.Security.Claims;

namespace Skanly.Web.Controllers;

[Authorize(Policy = "StudentOrOwner")]
public class ChatController : Controller
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Student chat page ─────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> StudentIndex(
        int? conversationId,
        CancellationToken ct)
    {
        var result = await _chatService.GetConversationsAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Dashboard", new { area = "Student" });
        }

        ViewBag.ConversationId = conversationId;
        ViewBag.CurrentUserId = UserId;

        return View("~/Areas/Student/Views/Chat/Index.cshtml", result.Data);
    }

    // ── Owner chat page ───────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> OwnerIndex(
        int? conversationId,
        CancellationToken ct)
    {
        var result = await _chatService.GetConversationsAsync(UserId, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Dashboard", new { area = "Owner" });
        }

        ViewBag.ConversationId = conversationId;
        ViewBag.CurrentUserId = UserId;

        return View("~/Areas/Owner/Views/Chat/Index.cshtml", result.Data);
    }

    // ── Start conversation (called from property detail "Chat" button) ─────────

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = "StudentOnly")]
    public async Task<IActionResult> Start(
        StartConversationDto dto,
        CancellationToken ct)
    {
        var result = await _chatService
            .GetOrCreateConversationAsync(UserId, dto, ct);

        if (!result.IsSuccess)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new
        {
            success = true,
            conversationId = result.Data!.ConversationId
        });
    }

    // ── Load messages (AJAX pagination) ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Messages(
        int conversationId,
        int page = 1,
        CancellationToken ct = default)
    {
        var result = await _chatService
            .GetMessagesAsync(UserId, conversationId, page, 40, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    // ── Upload image via HTTP (preferred over base64 through SignalR) ──────────

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        int conversationId,
        IFormFile image,
        CancellationToken ct)
    {
        var dto = new SendMessageDto
        {
            ConversationId = conversationId,
            Image = image
        };

        var result = await _chatService.SendMessageAsync(UserId, dto, ct);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    // ── Unread count (for nav badge polling) ──────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var result = await _chatService.GetTotalUnreadCountAsync(UserId, ct);
        return Ok(new { count = result.Data });
    }
}