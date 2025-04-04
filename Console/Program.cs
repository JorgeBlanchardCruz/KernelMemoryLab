using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Configuration;

namespace KernelMemoryLocalWeaviateDemo;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Configuración para LMStudio (chat)
        var lmStudioConfig = new OpenAIConfig
        {
            Endpoint = "http://localhost:1234/v1/",
            TextModel = "Mistral-7B-Instruct-v0.2-GGUF",
            TextModelMaxTokenTotal = 4096,
            APIKey = "sk-dummy-key" // Clave ficticia para pasar la validación
        };

        var embeddingConfig = new LMStudioEmbeddingConfig
        {
            Endpoint = "http://localhost:1234/v1/",
            EmbeddingModel = "nomic-embed-text-v1",
            APIKey = string.Empty // Sin autenticación en entorno local
        };

        // 2. Utilizamos un generador de embeddings local personalizado
        var embeddingGenerator = new LMStudioEmbeddingGenerator(embeddingConfig);

        var textPartitioningOptions = new TextPartitioningOptions
        {
            MaxTokensPerParagraph = 512 // Ajustar el tamaño de partición a 512 tokens
        };

        // 3. Construir KernelMemory con el generador de embeddings y el modelo de texto
        var memory = new KernelMemoryBuilder()
             .WithCustomEmbeddingGenerator(embeddingGenerator)
             .WithOpenAITextGeneration(lmStudioConfig)
             .WithCustomTextPartitioningOptions(textPartitioningOptions)
             .Build();

        // 4. Importar un documento de ejemplo (se usa para indexación de embeddings)
        string document = "Hoy es 1 de abril de 2025 y esta es una prueba local con KernelMemory y Weaviate.";
        await memory.ImportTextAsync(document, documentId: "doc-001");

        // Espera breve para permitir la ingestión del documento
        await Task.Delay(2000);

        // 5. Realizar una consulta mediante el chat (LMStudio se encargará de generar la respuesta)
        var answer = await memory.AskAsync("¿Cuál es la fecha actual?");
        Console.WriteLine("Pregunta: " + answer.Question);
        Console.WriteLine("Respuesta: " + answer.Result);

        // 6. Obtener el embedding del documento usando el generador local
        var embedding = await embeddingGenerator.GenerateEmbeddingAsync(document);

        // 7. Conexión e interacción con Weaviate (montado en puerto 8080)
        var weaviateClient = new WeaviateClient("http://localhost:8080");

        // Agregar el documento y su embedding a Weaviate (se hardcodea el nombre de la clase "Document")
        string weaviateDocId = await weaviateClient.AddDocumentAsync("Document", "doc-001", document, embedding);
        Console.WriteLine($"Documento almacenado en Weaviate con id: {weaviateDocId}");

        // 8. Realizar búsqueda en Weaviate usando el embedding generado para encontrar conceptos (por ejemplo, la palabra "fecha")
        var searchResult = await weaviateClient.SearchDocumentAsync("Document", "fecha", embedding);
        Console.WriteLine("Resultado de búsqueda en Weaviate:");
        Console.WriteLine(searchResult);
    }
}

