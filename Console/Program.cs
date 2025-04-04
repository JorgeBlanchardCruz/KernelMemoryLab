using Microsoft.Extensions.Logging;
using RAGWithKernelMemory;
using WeaviateNET;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Iniciando aplicación RAG con Kernel.Memory y Weaviate");

// Configuración de Weaviate (asumiendo que está corriendo en localhost:8080)
var weaviateEndpoint = "http://localhost:8080";
var weaviateClient = new WeaviateDB(weaviateEndpoint);

// Configuración de LM Studio (asumiendo que está ejecutándose con la API en localhost:1234)
var lmStudioEndpoint = "http://localhost:1234/v1";

// Configurar el cliente de embeddings
var embeddingClient = new OpenAICompatibleEmbeddingGenerator(
    new Uri("http://localhost:11434/api/embeddings"), // Puerto de Ollama
    "nomic-embed-text", // Nombre del modelo en Ollama
    dimension: 768, // Dimensión específica para nomic-embed-text
    apiKey: "ollama", // Puede ser cualquier valor para Ollama
    loggerFactory: loggerFactory);

// Configurar el servicio de memorias con Weaviate
var weaviateStore = new WeaviateMemoryStore(
    weaviateClient,
    embeddingClient,
    loggerFactory.CreateLogger<WeaviateMemoryStore>());
