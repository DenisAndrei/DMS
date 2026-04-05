namespace DeviceManagement.Api.Options;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; init; } = "Ollama";

    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";

    public string Model { get; init; } = "phi4-mini";

    public bool UseFallbackWhenUnavailable { get; init; } = true;

    public int RequestTimeoutSeconds { get; init; } = 30;
}
