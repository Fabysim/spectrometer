using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Spectrometre.Core.Ai;

/// <summary>
/// Implémentation des appels à l'API Replicate (ex. Claude) — reprise de mvp
/// (<c>Spectrometre.Services.ReplicateService</c>) : même modèle, mêmes timeouts, même clé de config.
/// </summary>
public sealed class ReplicateService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IReplicateService
{
    private const string ReplicateBaseUrl = "https://api.replicate.com/v1/";
    private const string ClaudeModel = "anthropic/claude-4-sonnet";
    private const string DefaultWhisperModel =
        "openai/whisper:3c08daf437fe359eb158a5123c395673f0a113dd8b4bd01ddce5936850e2a981";

    /// <inheritdoc />
    public async Task<(string? Output, string? Error)> RunClaudeAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var token = GetApiToken();
        if (string.IsNullOrWhiteSpace(token))
            return (null, "Service IA non configuré. Configurez Replicate:ApiToken ou la variable REPLICATE_API_TOKEN.");

        try
        {
            using var client = GetClient(token);
            var predictionId = await CreateClaudePredictionAsync(client, systemPrompt, userPrompt, cancellationToken);
            var outputText = await WaitForClaudePredictionAsync(client, predictionId, cancellationToken);
            return (outputText, null);
        }
        catch (HttpRequestException ex)
        {
            return (null, "Erreur de connexion à l'API Replicate : " + ex.Message);
        }
        catch (TaskCanceledException)
        {
            return (null, "La requête a expiré. Veuillez réessayer.");
        }
        catch (Exception ex)
        {
            return (null, "Erreur : " + ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(string? Transcript, string? Error)> TranscribeAudioAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        var token = GetApiToken();
        if (string.IsNullOrWhiteSpace(token))
            return (null, "Service IA non configuré. Configurez Replicate:ApiToken ou la variable REPLICATE_API_TOKEN.");

        if (audioBytes.Length == 0)
            return (null, "Aucun audio à transcrire.");

        try
        {
            using var client = GetClient(token);
            var maxAudioBytes = GetWhisperMaxAudioBytes();
            if (audioBytes.Length > maxAudioBytes)
            {
                var sizeMo = audioBytes.Length / 1024.0 / 1024.0;
                var maxMo = maxAudioBytes / 1024.0 / 1024.0;
                return (null, $"Audio trop volumineux ({sizeMo:0.#} Mo, maximum {maxMo:0.#} Mo). Raccourcissez l'entrevue.");
            }

            var audioInput = BuildAudioDataUri(audioBytes, mimeType);
            var request = new
            {
                version = GetWhisperModel(),
                input = new
                {
                    audio = audioInput,
                    transcription = "plain text",
                    language = GetWhisperLanguage(),
                },
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("predictions", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                return (null, $"Replicate (création) : {(int)response.StatusCode} — {err}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var createDoc = JsonDocument.Parse(responseJson);
            var predictionId = createDoc.RootElement.GetProperty("id").GetString();
            if (string.IsNullOrEmpty(predictionId))
                return (null, "Réponse Replicate invalide (pas d'identifiant de prédiction).");

            const int maxAttempts = 60;
            const int delayMs = 1500;
            for (var i = 0; i < maxAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(delayMs, cancellationToken);

                var pollResponse = await client.GetAsync($"predictions/{predictionId}", cancellationToken);
                if (!pollResponse.IsSuccessStatusCode)
                    continue;

                var pollJson = await pollResponse.Content.ReadAsStringAsync(cancellationToken);
                using var pollDoc = JsonDocument.Parse(pollJson);
                var root = pollDoc.RootElement;
                var status = root.GetProperty("status").GetString();

                if (status == "succeeded")
                {
                    if (!root.TryGetProperty("output", out var output))
                        return (string.Empty, null);

                    return (ExtractWhisperTranscript(output), null);
                }

                if (status is "failed" or "canceled")
                {
                    var err = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;
                    return (null, err ?? "Échec de la transcription Whisper.");
                }
            }

            return (null, "Délai d'attente dépassé pour la transcription.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return (null, null);
        }
        catch (TaskCanceledException)
        {
            return (null, "La requête a expiré. Veuillez réessayer.");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    private string? GetApiToken() =>
        configuration["Replicate:ApiToken"] ?? Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");

    private string GetWhisperModel() =>
        configuration["Replicate:WhisperModel"]
        ?? Environment.GetEnvironmentVariable("REPLICATE_WHISPER_MODEL")
        ?? DefaultWhisperModel;

    private string GetWhisperLanguage() =>
        configuration["Replicate:WhisperLanguage"]
        ?? Environment.GetEnvironmentVariable("REPLICATE_WHISPER_LANGUAGE")
        ?? "fr";

    private int GetWhisperMaxAudioBytes()
    {
        var configured = configuration["Replicate:WhisperMaxAudioBytes"]
            ?? Environment.GetEnvironmentVariable("REPLICATE_WHISPER_MAX_AUDIO_BYTES");
        if (int.TryParse(configured, out var bytes) && bytes > 0)
            return bytes;
        return 8_000_000;
    }

    private HttpClient GetClient(string token)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(ReplicateBaseUrl);
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        return client;
    }

    private static string BuildAudioDataUri(byte[] audioBytes, string mimeType)
    {
        var normalizedMime = NormalizeAudioMimeType(mimeType);
        return $"data:{normalizedMime};base64,{Convert.ToBase64String(audioBytes)}";
    }

    private static string NormalizeAudioMimeType(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return "audio/webm";

        var baseType = mimeType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrEmpty(baseType) ? "audio/webm" : baseType;
    }

    private static string? ExtractWhisperTranscript(JsonElement output)
    {
        if (output.ValueKind == JsonValueKind.String)
            return output.GetString();

        if (output.ValueKind == JsonValueKind.Object)
        {
            if (output.TryGetProperty("transcription", out var transcription))
                return transcription.GetString();
            if (output.TryGetProperty("text", out var text))
                return text.GetString();
        }

        return output.ToString();
    }

    private static async Task<string> CreateClaudePredictionAsync(
        HttpClient httpClient, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var request = new
        {
            input = new
            {
                prompt = userPrompt,
                system_prompt = systemPrompt,
                max_tokens = 4096,
            },
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"models/{ClaudeModel}/predictions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Replicate API error ({(int)response.StatusCode}): {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Missing prediction id");
    }

    private static async Task<string> WaitForClaudePredictionAsync(
        HttpClient httpClient, string predictionId, CancellationToken cancellationToken)
    {
        const int maxAttempts = 120;
        const int delayMs = 2500;
        for (var i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await httpClient.GetAsync($"predictions/{predictionId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var status = root.GetProperty("status").GetString();
            if (status == "succeeded")
            {
                if (!root.TryGetProperty("output", out var output))
                    return string.Empty;
                if (output.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var el in output.EnumerateArray())
                        sb.Append(el.GetString());
                    return sb.ToString().Trim();
                }

                return output.GetString() ?? string.Empty;
            }

            if (status is "failed" or "canceled")
            {
                var err = root.TryGetProperty("error", out var errProp) ? errProp.GetString() : "Unknown error";
                throw new Exception($"Prediction failed: {err}");
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        throw new TimeoutException("La génération a pris trop de temps.");
    }
}
