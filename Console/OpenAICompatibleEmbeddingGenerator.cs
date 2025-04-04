using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RAGWithKernelMemory;

public class OpenAICompatibleEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private readonly int _dimension;
    private readonly ILogger<OpenAICompatibleEmbeddingGenerator> _logger;

    public int MaxTokens => throw new NotImplementedException();

    public OpenAICompatibleEmbeddingGenerator(
        Uri endpoint,
        string modelId,
        int dimension,
        string apiKey,
        ILoggerFactory? loggerFactory = null)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = endpoint;
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _modelId = modelId;
        _dimension = dimension;
        _logger = loggerFactory?.CreateLogger<OpenAICompatibleEmbeddingGenerator>() ??
                  Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAICompatibleEmbeddingGenerator>.Instance;
    }

    public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GenerateEmbeddingInternalAsync(text, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    private async Task<Embedding> GenerateEmbeddingInternalAsync(string text, CancellationToken cancellationToken)
    {
        var request = new
        {
            model = _modelId,
            input = text,
            encoding_format = "float"
        };

        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseBody);

        if (responseObject.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0 &&
            data[0].TryGetProperty("embedding", out var embeddingElement) &&
            embeddingElement.ValueKind == JsonValueKind.Array)
        {
            var embeddingArray = new float[embeddingElement.GetArrayLength()];
            for (int i = 0; i < embeddingArray.Length; i++)
            {
                embeddingArray[i] = embeddingElement[i].GetSingle();
            }

            return new Embedding(embeddingArray);
        }

        throw new Exception("Invalid response format from embedding service");
    }

    public int GetDimension() => _dimension;

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<ReadOnlyMemory<float>>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            results.Add(embedding);
        }

        return results;
    }

    public async Task<IList<Embedding>> GenerateEmbeddingsAsync(IList<string> texts, bool normalize, CancellationToken cancellationToken = default)
    {
        var results = new List<Embedding>();

        foreach (var text in texts)
        {
            var embedding = await ((ITextEmbeddingGenerator)this).GenerateEmbeddingAsync(text, cancellationToken);
            results.Add(embedding);
        }

        return results;
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Una implementación simple para contar tokens
        // Esto es una aproximación básica; los modelos de OpenAI tienen algoritmos más complejos
        // para tokenización basados en BPE (Byte Pair Encoding)

        // Dividir por espacios, puntuación y otros separadores comunes
        var tokens = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '"', '\'' },
                               StringSplitOptions.RemoveEmptyEntries);

        _logger.LogDebug($"Texto tokenizado con {tokens.Length} tokens aproximados");
        return tokens.Length;
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        // Implementación simple de tokenización
        // Nota: Esta es una aproximación que no refleja exactamente cómo OpenAI tokeniza el texto
        // Para una tokenización precisa se necesitaría implementar o utilizar el tokenizador específico del modelo

        var tokens = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '"', '\'' },
                               StringSplitOptions.RemoveEmptyEntries);

        _logger.LogDebug($"Texto dividido en {tokens.Length} tokens");
        return tokens;
    }
}