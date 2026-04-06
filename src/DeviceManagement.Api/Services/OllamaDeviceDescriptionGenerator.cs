using System.Text;
using System.Text.Json;
using DeviceManagement.Api.Domain;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Options;
using Microsoft.Extensions.Options;

namespace DeviceManagement.Api.Services;

public sealed class OllamaDeviceDescriptionGenerator : IDeviceDescriptionGenerator
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<OllamaDeviceDescriptionGenerator> _logger;

    public OllamaDeviceDescriptionGenerator(
        HttpClient httpClient,
        IOptions<AiOptions> aiOptions,
        ILogger<OllamaDeviceDescriptionGenerator> logger)
    {
        _httpClient = httpClient;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    public async Task<DeviceDescriptionResult> GenerateAsync(
        DeviceDescriptionInput input,
        CancellationToken cancellationToken)
    {
        var request = new OllamaGenerateRequest(
            _aiOptions.Model,
            BuildPrompt(input),
            Stream: false);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (_aiOptions.UseFallbackWhenUnavailable)
                {
                    return BuildFallbackResult(
                        input,
                        $"Ollama request failed with status code {(int)response.StatusCode}.");
                }

                response.EnsureSuccessStatusCode();
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload?.Response))
            {
                return BuildFallbackResult(input, "Ollama returned an empty description.");
            }

            return new DeviceDescriptionResult(
                payload.Response.Trim(),
                Provider: "Ollama",
                Model: _aiOptions.Model,
                UsedFallback: false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BuildFallbackResult(input, "Ollama timed out while generating the description.");
        }
        catch (HttpRequestException exception)
        {
            return BuildFallbackResult(input, $"Ollama could not be reached: {exception.Message}");
        }
        catch (JsonException exception)
        {
            return BuildFallbackResult(input, $"Ollama returned an unexpected response: {exception.Message}");
        }
    }

    private DeviceDescriptionResult BuildFallbackResult(DeviceDescriptionInput input, string reason)
    {
        // Use a simple fallback so the feature still works when Ollama is not available locally.
        _logger.LogWarning("Using fallback device description generator. Reason: {Reason}", reason);

        return new DeviceDescriptionResult(
            BuildTemplateDescription(input),
            Provider: "TemplateFallback",
            Model: "deterministic-template",
            UsedFallback: true);
    }

    private static string BuildPrompt(DeviceDescriptionInput input)
    {
        var typeLabel = input.Type == DeviceType.Phone ? "smartphone" : "tablet";
        var builder = new StringBuilder();
        builder.AppendLine("Write one concise, human-readable device description for a company inventory system.");
        builder.AppendLine("Keep it to one sentence, no bullet points, no markdown.");
        builder.AppendLine("Focus on business usefulness and the device profile.");
        builder.AppendLine();
        builder.AppendLine($"Name: {input.Name}");
        builder.AppendLine($"Manufacturer: {input.Manufacturer}");
        builder.AppendLine($"Type: {typeLabel}");
        builder.AppendLine($"Operating system: {input.OperatingSystem}");
        builder.AppendLine($"OS version: {input.OsVersion}");
        builder.AppendLine($"Processor: {input.Processor}");
        builder.AppendLine($"RAM: {input.RamAmountGb} GB");

        return builder.ToString();
    }

    private static string BuildTemplateDescription(DeviceDescriptionInput input)
    {
        var typePhrase = input.Type == DeviceType.Phone ? "smartphone" : "tablet";
        var ramPhrase = input.RamAmountGb >= 12 ? "high-memory" : input.RamAmountGb >= 8 ? "capable" : "compact";

        return $"A {ramPhrase} {input.Manufacturer} {typePhrase} running {input.OperatingSystem} {input.OsVersion}, suitable for daily business use.";
    }

    private sealed record OllamaGenerateRequest(
        string Model,
        string Prompt,
        bool Stream);

    private sealed record OllamaGenerateResponse(
        string Response);
}
