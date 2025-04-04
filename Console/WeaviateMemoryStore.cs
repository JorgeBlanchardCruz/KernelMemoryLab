using Elastic.Clients.Elasticsearch.Tasks;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.MemoryStorage;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RAGWithKernelMemory;

public class WeaviateMemoryStore : IMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private const string DefaultClassName = "KernelMemory";

    private class SchemaResponse
    {
        public List<ClassInfo> Classes { get; set; }
    }

    private class ClassInfo
    {
        public string ClassName { get; set; }
    }

    private class UpsertResponse
    {
        public string Id { get; set; }
    }

    private class SearchResponse
    {
        public List<SearchResult> Results { get; set; }
    }

    private class SearchResult
    {
        public string Id { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public double Score { get; set; }
        public float[] Vector { get; set; }
    }

    private class ListResponse
    {
        public List<ListResult> Results { get; set; }
    }

    private class ListResult
    {
        public string Id { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public float[] Vector { get; set; }
    }

    public WeaviateMemoryStore(ITextEmbeddingGenerator embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/schema";
            var requestBody = new
            {
                className = index,
                vectorIndexConfig = new
                {
                    vectorSize = vectorSize
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/schema";
            var response = await httpClient.GetAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var schemaResponse = JsonSerializer.Deserialize<SchemaResponse>(responseContent);

            return schemaResponse.Classes.Select(c => c.ClassName);
        }
    }

    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/schema/{index}";
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/objects";
            var requestBody = new
            {
                className = index,
                properties = new
                {
                    record.Id,
                    record.Tags,
                },
                vector = record.Vector
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var upsertResponse = JsonSerializer.Deserialize<UpsertResponse>(responseContent);

            return upsertResponse.Id;
        }
    }

    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(string index, string text, ICollection<MemoryFilter>? filters = null, double minRelevance = 0, int limit = 1, bool withEmbeddings = false, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken);

        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/objects/search";
            var requestBody = new
            {
                className = index,
                vector = embedding,
                limit = limit,
                minRelevance = minRelevance,
                withEmbeddings = withEmbeddings,
                filters = filters
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResponse = JsonSerializer.Deserialize<SearchResponse>(responseContent);

            foreach (var result in searchResponse.Results)
            {
                yield return (new MemoryRecord
                {
                    Id = result.Id,
                    Tags = (TagCollection)result.Properties["Tags"],
                    Vector = result.Vector
                }, result.Score);
            }
        }
    }

    public async IAsyncEnumerable<MemoryRecord> GetListAsync(string index, ICollection<MemoryFilter>? filters = null, int limit = 1, bool withEmbeddings = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/objects";
            var requestBody = new
            {
                className = index,
                limit = limit,
                withEmbeddings = withEmbeddings,
                filters = filters
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, jsonContent, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var listResponse = JsonSerializer.Deserialize<ListResponse>(responseContent);

            foreach (var result in listResponse.Results)
            {
                yield return new MemoryRecord
                {
                    Id = result.Id,
                    Tags = (TagCollection)result.Properties["Tags"],
                    Vector = result.Vector
                };
            }
        }
    }

    public async Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        using (var httpClient = new HttpClient())
        {
            var requestUri = $"http://localhost:8080/v1/objects/{index}/{record.Id}";
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }


}