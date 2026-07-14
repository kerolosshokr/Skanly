// Skanly.Application/Features/Chatbot/Interfaces/IClaudeChatClient.cs
using Skanly.Application.Features.Chatbot.DTOs;

namespace Skanly.Application.Features.Chatbot.Interfaces;

public interface IClaudeChatClient
{
    /// <summary>
    /// Sends a conversation to Claude and returns the full response.
    /// </summary>
    Task<ClaudeResponse?> SendAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> history,
        string userMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a conversation to Claude and streams the response
    /// token-by-token via the provided callback.
    /// </summary>
    Task StreamAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> history,
        string userMessage,
        Func<string, Task> onToken,
        Func<int, Task> onComplete,
        CancellationToken ct = default);
}

public record ClaudeMessage(string Role, string Content);

public class ClaudeResponse
{
    public string Content { get; init; } = string.Empty;
    public int TokensUsed { get; init; }
}