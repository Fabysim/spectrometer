using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Implémentation réelle de <see cref="IAiSynthesisService"/> — appelle Claude via l'API Replicate, comme
/// mvp (<c>ReplicateService.RunClaudeAsync</c>) : mêmes clés de configuration
/// (<c>Replicate:ApiToken</c>/variable d'environnement <c>REPLICATE_API_TOKEN</c>, jamais en dur), même
/// modèle, même mécanique de prédiction asynchrone (créer puis interroger). Reprise volontaire de ces clés
/// exactes plutôt que d'en introduire de nouvelles, pour ne pas dupliquer le provisionnement de secret côté
/// déploiement pour une variable qui existe déjà si Replicate est utilisé ailleurs.
/// </summary>
public sealed class ReplicateAiSynthesisService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IAiSynthesisService
{
    private const string ReplicateBaseUrl = "https://api.replicate.com/v1/";
    private const string ClaudeModel = "anthropic/claude-4-sonnet";

    public async Task<(string? Output, string? Error)> GenererTexteAsync(
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
            var predictionId = await CreatePredictionAsync(client, systemPrompt, userPrompt, cancellationToken);
            var output = await WaitForPredictionAsync(client, predictionId, cancellationToken);
            return (output, null);
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

    private string? GetApiToken() =>
        configuration["Replicate:ApiToken"] ?? Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");

    private HttpClient GetClient(string token)
    {
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(ReplicateBaseUrl);
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        return client;
    }

    private static async Task<string> CreatePredictionAsync(HttpClient client, string systemPrompt, string userPrompt, CancellationToken cancellationToken)
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
        var response = await client.PostAsync($"models/{ClaudeModel}/predictions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Replicate API error ({(int)response.StatusCode}): {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("id").GetString() ?? throw new InvalidOperationException("Missing prediction id");
    }

    private static async Task<string> WaitForPredictionAsync(HttpClient client, string predictionId, CancellationToken cancellationToken)
    {
        const int maxAttempts = 120;
        const int delayMs = 2500;

        for (var i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await client.GetAsync($"predictions/{predictionId}", cancellationToken);
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
