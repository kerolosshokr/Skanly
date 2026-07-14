// Skanly.Infrastructure/AI/ClaudeChatClient.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skanly.Application.Features.Chatbot.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace Skanly.Infrastructure.AI;

public class ClaudeChatClient : IClaudeChatClient
{
    private readonly HttpClient _http;
    private readonly ClaudeClientSettings _settings;
    private readonly ILogger<ClaudeChatClient> _logger;

    private const string ApiUrl = "https://api.anthropic.com/v1/messages";

    public ClaudeChatClient(
        HttpClient http,
        IOptions<ClaudeClientSettings> settings,
        ILogger<ClaudeChatClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // ── Non-streaming ─────────────────────────────────────────────────────────

    public async Task<ClaudeResponse?> SendAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> history,
        string userMessage,
        CancellationToken ct = default)
    {
        var messages = BuildMessages(history, userMessage);

        var requestBody = new
        {
            model = _settings.Model,
            max_tokens = _settings.MaxTokens,
            system = systemPrompt,
            messages
        };

        try
        {
            using var cts = CancellationTokenSource
                .CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            var response = await _http.PostAsJsonAsync(
                ApiUrl, requestBody, cts.Token);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(body);

            var content = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var inputTokens = doc.RootElement
                .GetProperty("usage")
                .GetProperty("input_tokens").GetInt32();
            var outputTokens = doc.RootElement
                .GetProperty("usage")
                .GetProperty("output_tokens").GetInt32();

            return new ClaudeResponse
            {
                Content = content,
                TokensUsed = inputTokens + outputTokens
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Claude chatbot API timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude chatbot API error");
            return null;
        }
    }

    // ── Streaming ─────────────────────────────────────────────────────────────

    public async Task StreamAsync(
        string systemPrompt,
        IReadOnlyList<ClaudeMessage> history,
        string userMessage,
        Func<string, Task> onToken,
        Func<int, Task> onComplete,
        CancellationToken ct = default)
    {
        var messages = BuildMessages(history, userMessage);

        var requestBody = new
        {
            model = _settings.Model,
            max_tokens = _settings.MaxTokens,
            system = systemPrompt,
            stream = true,
            messages
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(
                jsonContent,
                System.Text.Encoding.UTF8,
                "application/json")
        };

        try
        {
            using var cts = CancellationTokenSource
                .CreateLinkedTokenSource(ct);
            cts.CancelAfter(
                TimeSpan.FromSeconds(_settings.TimeoutSeconds + 30));

            using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content
                .ReadAsStreamAsync(cts.Token);
            using var reader = new System.IO.StreamReader(stream);

            int totalOutputTokens = 0;

            string? line;

            while (!cts.Token.IsCancellationRequested &&
                   (line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var data = line["data: ".Length..];
                if (data == "[DONE]") break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var eventType = doc.RootElement
                        .GetProperty("type").GetString();

                    if (eventType == "content_block_delta")
                    {
                        var delta = doc.RootElement
                            .GetProperty("delta");

                        if (delta.GetProperty("type").GetString()
                            == "text_delta")
                        {
                            var token = delta.GetProperty("text")
                                             .GetString() ?? "";
                            if (!string.IsNullOrEmpty(token))
                                await onToken(token);
                        }
                    }
                    else if (eventType == "message_delta")
                    {
                        if (doc.RootElement.TryGetProperty(
                            "usage", out var usage))
                        {
                            totalOutputTokens = usage
                                .GetProperty("output_tokens")
                                .GetInt32();
                        }
                    }
                }
                catch (JsonException)
                {
                    // Malformed SSE chunk — skip
                }
            }

            await onComplete(totalOutputTokens);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Claude chatbot stream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude chatbot streaming error");
            await onToken("\n\n*I ran into an issue. Please try again.*");
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static object[] BuildMessages(
        IReadOnlyList<ClaudeMessage> history,
        string userMessage)
    {
        var messages = history
            .Select(m => new { role = m.Role, content = m.Content })
            .Cast<object>()
            .ToList();

        messages.Add(new { role = "user", content = userMessage });
        return messages.ToArray();
    }
}