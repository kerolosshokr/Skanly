// Skanly.Infrastructure/AI/ClaudeClientSettings.cs
namespace Skanly.Infrastructure.AI;

public class ClaudeClientSettings
{
    public const string SectionName = "ClaudeClient";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "claude-sonnet-4-6";
    public int MaxTokens { get; init; } = 1000;
    public int TimeoutSeconds { get; init; } = 15;
    public int CacheMinutes { get; init; } = 30;
    public bool EnableAiRefinement { get; init; } = true;
    public int MinPropertiesForAi { get; init; } = 3;
}