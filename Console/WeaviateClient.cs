using Microsoft.KernelMemory;
using System.Text;
using System.Text.Json;

namespace KernelMemoryLocalWeaviateDemo;

// Cliente sencillo para interactuar con Weaviate en local mediante sus endpoints REST.
public class WeaviateClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public WeaviateClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _client = new HttpClient()
        {
            BaseAddress = new Uri(_baseUrl)
        };
    }

    // Agrega un documento a Weaviate usando la ruta /v1/objects
    public async Task<string> AddDocumentAsync(string className, string docId, string text, Embedding embedding)
    {
        var payload = new
        {
            @class = className,
            id = docId,
            properties = new
            {
                text = text
            },
            vector = embedding.Data // Se presupone que Embedding.Vector es un array float[]
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1/objects", content);
        response.EnsureSuccessStatusCode();
        return docId;
    }

    // Realiza una búsqueda de documento similar usando GraphQL (endpoint /v1/graphql)
    public async Task<string> SearchDocumentAsync(string className, string searchTerm, Embedding searchEmbedding)
    {
        // Se construye una consulta GraphQL simple; para producción se ajusta el parámetro "distance" según sea necesario.
        string query = $@"
        {{
          Get {{
            {className}(nearVector: {{
              vector: {JsonSerializer.Serialize(searchEmbedding.Data)},
              distance: 0.5
            }}) {{
              text
              _additional {{
                id
              }}
            }}
          }}
        }}
        ";

        var payload = new { query = query };
        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1/graphql", content);
        response.EnsureSuccessStatusCode();
        string result = await response.Content.ReadAsStringAsync();
        return result;
    }
}
