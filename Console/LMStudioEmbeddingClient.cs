using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using System.Text;
using System.Text.Json;

namespace KernelMemoryLocalWeaviateDemo;

public class LMStudioEmbeddingConfig
{
    public string Endpoint { get; set; } = default!;
    public string EmbeddingModel { get; set; } = default!;
    public string APIKey { get; set; } = default!;
}

public class LMStudioEmbeddingClient
{
    private readonly HttpClient _client;
    private readonly LMStudioEmbeddingConfig _config;

    public LMStudioEmbeddingClient(LMStudioEmbeddingConfig config)
    {
        _config = config;
        _client = new HttpClient { BaseAddress = new Uri(_config.Endpoint) };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var payload = new { input = text, model = _config.EmbeddingModel };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Se asume que el endpoint es: /embeddings
        var response = await _client.PostAsync("embeddings", content);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Se espera que la respuesta tenga la propiedad "embedding" con un array de números.
        var embedding = doc.RootElement.GetProperty("data")
                           .EnumerateArray()
                           .First()
                           .GetProperty("embedding")
                           .EnumerateArray()
                           .Select(e => e.GetSingle())
                           .ToArray();
        return embedding;
    }
}

public class LMStudioEmbeddingGenerator : ITextEmbeddingGenerator
{
    private readonly LMStudioEmbeddingClient _embeddingClient;

    // Por ejemplo, definimos un máximo de tokens (ajústalo según el modelo)
    public int MaxTokens => 512;

    public LMStudioEmbeddingGenerator(LMStudioEmbeddingConfig config)
    {
        _embeddingClient = new LMStudioEmbeddingClient(config);
    }

    public int CountTokens(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public IReadOnlyList<string> GetTokens(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray();
    }

    public async Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        float[] vector = await _embeddingClient.GenerateEmbeddingAsync(text);
        return new Embedding(vector);
    }

}
