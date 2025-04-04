using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace KnowledgeIndexer;

//public class KnowledgeIndexer
//{
//    private readonly AzureOpenAIConfig _azureOpenAiTextConfig;
//    private readonly AzureOpenAIConfig _azureOpenAiEmbeddingConfig;
//    private readonly KernelMemoryConfig _kernelMemoryConfig;
//    private readonly KnowledgeIndexerOptions _options;

//    public KnowledgeIndexer(
//        IOptions<KnowledgeIndexerOptions> options
//        )
//    {
//        _options = options.Value;

//        _azureOpenAiTextConfig = new();
//        _azureOpenAiEmbeddingConfig = new();
//        _kernelMemoryConfig = new();
//    }

//    public async Task RunAsync(CancellationToken cancellationToken)
//    {
//        _logger.LogInformation("Knowledge Indexer is running.");
//        while (!cancellationToken.IsCancellationRequested)
//        {
//            try
//            {
//                var kernelMemory = await _kernelMemoryService.GetKernelMemoryAsync(cancellationToken);
//                var kernelMemoryAI = _kernelMemoryAIService.GetKernelMemoryAI(kernelMemory);
//                // Index the kernel memory AI
//                // ...
//                await Task.Delay(_options.Interval, cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while indexing the kernel memory AI.");
//                await Task.Delay(_options.ErrorInterval, cancellationToken);
//            }
//        }
//        _logger.LogInformation("Knowledge Indexer is stopping.");
//    }

//}
