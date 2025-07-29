# AiGeekSquad.AIContext.MEAI

A Microsoft Extensions AI Abstractions adapter for the AiGeekSquad.AIContext semantic chunking library.

## Overview

This package provides seamless integration between Microsoft's AI abstractions [`Microsoft.Extensions.AI.Abstractions`](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/) and the AiGeekSquad.AIContext library. It allows you to use any embedding generator that implements Microsoft's [`IEmbeddingGenerator<TInput,TEmbedding>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.iembeddinggenerator-2) interface with the AIContext semantic chunking functionality.

## Purpose

The [`MicrosoftExtensionsAIEmbeddingGenerator`](../AiGeekSquad.AIContext.MEAI/MicrosoftExtensionsAiEmbeddingGenerator.cs) class acts as an adapter that:

- Implements the [`AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator`](../AiGeekSquad.AIContext/Chunking/IEmbeddingGenerator.cs) interface
- Wraps any Microsoft Extensions AI embedding generator
- Converts between Microsoft's [`Embedding<float>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.embedding-1) format and Math.NET's [`Vector<double>`](https://numerics.mathdotnet.com/api/MathNet.Numerics.LinearAlgebra/Vector_1.htm) format
- Enables seamless integration with AIContext's semantic text chunking capabilities

## Installation

```bash
dotnet add package AiGeekSquad.AIContext.MEAI
```

## Usage

### Basic Usage

```csharp
using AiGeekSquad.AIContext.MEAI;
using AiGeekSquad.AIContext.Chunking;
using Microsoft.Extensions.AI;

// Initialize your Microsoft Extensions AI embedding generator
// This could be OpenAI, Azure OpenAI, or any other provider
IEmbeddingGenerator<string, Embedding<float>> microsoftEmbeddingGenerator =
    CreateYourEmbeddingGenerator(); // Your specific implementation

// Wrap it with the adapter
IEmbeddingGenerator aiContextEmbeddingGenerator =
    new MicrosoftExtensionsAIEmbeddingGenerator(microsoftEmbeddingGenerator);

// Create additional required components
var tokenCounter = new MLTokenCounter();
var similarityCalculator = new MathNetSimilarityCalculator();
var textSplitter = new SentenceTextSplitter();

// Use with AIContext semantic chunking
var chunker = new SemanticTextChunker(
    embeddingGenerator: aiContextEmbeddingGenerator,
    tokenCounter: tokenCounter,
    similarityCalculator: similarityCalculator,
    textSplitter: textSplitter
);

var text = "Your long document text that needs to be chunked into semantic segments...";
var chunks = await chunker.ChunkTextAsync(text);

// Process the results
foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk ({chunk.Text.Length} chars): {chunk.Text[..Math.Min(50, chunk.Text.Length)]}...");
}
```

### With Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AiGeekSquad.AIContext.MEAI;
using AiGeekSquad.AIContext.Chunking;
using Microsoft.Extensions.AI;

var builder = Host.CreateApplicationBuilder(args);

// Register your Microsoft Extensions AI embedding generator
// Example: Register OpenAI embedding generator
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(provider =>
{
    // Your specific embedding generator implementation
    return CreateYourEmbeddingGenerator(); // Replace with actual implementation
});

// Register AIContext dependencies
builder.Services.AddSingleton<ITokenCounter, MLTokenCounter>();
builder.Services.AddSingleton<ISimilarityCalculator, MathNetSimilarityCalculator>();
builder.Services.AddSingleton<ITextSplitter, SentenceTextSplitter>();

// Register the adapter
builder.Services.AddSingleton<IEmbeddingGenerator, MicrosoftExtensionsAIEmbeddingGenerator>();

// Register semantic chunker with all dependencies
builder.Services.AddSingleton<SemanticTextChunker>();

var app = builder.Build();

// Use the chunker
var chunker = app.Services.GetRequiredService<SemanticTextChunker>();
var chunks = await chunker.ChunkTextAsync("Your document text...");
```

### Advanced Example with Custom Configuration

```csharp
using AiGeekSquad.AIContext.MEAI;
using AiGeekSquad.AIContext.Chunking;
using Microsoft.Extensions.AI;

// Initialize your Microsoft Extensions AI embedding generator
IEmbeddingGenerator<string, Embedding<float>> microsoftGenerator =
    CreateYourEmbeddingGenerator(); // Your implementation

// Create the adapter
var embeddingGenerator = new MicrosoftExtensionsAIEmbeddingGenerator(microsoftGenerator);

// Configure chunking options for optimal performance
var chunkOptions = new ChunkOptions
{
    MaxChunkSize = 1000,           // Maximum tokens per chunk
    OverlapSize = 100,             // Overlap between chunks
    SimilarityThreshold = 0.75,    // Semantic similarity threshold
    MinChunkSize = 50              // Minimum viable chunk size
};

// Create required components
var tokenCounter = new MLTokenCounter();
var similarityCalculator = new MathNetSimilarityCalculator();
var textSplitter = new SentenceTextSplitter();

// Create semantic chunker with all dependencies
var chunker = new SemanticTextChunker(
    embeddingGenerator: embeddingGenerator,
    tokenCounter: tokenCounter,
    similarityCalculator: similarityCalculator,
    textSplitter: textSplitter,
    options: chunkOptions
);

// Process a long document
var text = @"Your long document text here. This could be a research paper,
             technical documentation, or any lengthy content that needs to be
             semantically chunked for better processing...";

var chunks = await chunker.ChunkTextAsync(text);

// Display results with detailed information
Console.WriteLine($"Document chunked into {chunks.Count} semantic segments:");
for (int i = 0; i < chunks.Count; i++)
{
    var chunk = chunks[i];
    Console.WriteLine($"\n--- Chunk {i + 1} ---");
    Console.WriteLine($"Length: {chunk.Text.Length} characters");
    Console.WriteLine($"Text: {chunk.Text[..Math.Min(100, chunk.Text.Length)]}...");
    Console.WriteLine($"Embedding dimensions: {chunk.Embedding.Count}");
    Console.WriteLine($"First 5 embedding values: [{string.Join(", ", chunk.Embedding.Take(5).Select(v => v.ToString("F4")))}...]");
}
```

## Integration Benefits

By using this adapter, you gain several key advantages:

1. **Leverage Microsoft's AI Ecosystem**: Use any embedding generator that follows Microsoft's AI abstractions, including OpenAI, Azure OpenAI, and other providers
2. **Maintain Compatibility**: Keep your existing AIContext semantic chunking code unchanged while upgrading your embedding provider
3. **Future-Proof Architecture**: Benefit from updates to both Microsoft's AI abstractions and AIContext libraries without breaking changes
4. **Optimized Performance**: Take advantage of Microsoft's optimized embedding implementations and async patterns
5. **Provider Flexibility**: Switch between different embedding providers without changing your chunking logic
6. **Type Safety**: Enjoy full compile-time type checking and IntelliSense support

## Supported Operations

The adapter supports both single and batch embedding generation with full async support:

### Single Embedding Generation
```csharp
// Generate embedding for a single text
var embedding = await embeddingGenerator.GenerateEmbeddingAsync("Your text here");
```

### Batch Embedding Generation
```csharp
// Generate embeddings for multiple texts efficiently
var texts = new[] { "First text", "Second text", "Third text" };
var embeddings = await embeddingGenerator.GenerateBatchEmbeddingsAsync(texts);
```

Both methods automatically convert from Microsoft's [`Embedding<float>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.embedding-1) format to Math.NET's [`Vector<double>`](https://numerics.mathdotnet.com/api/MathNet.Numerics.LinearAlgebra/Vector_1.htm) format as required by the AIContext library.

## Error Handling

The adapter provides comprehensive error handling and validation:

- **Null Argument Validation**: Throws [`ArgumentNullException`](https://docs.microsoft.com/en-us/dotnet/api/system.argumentnullexception) for null inputs with descriptive parameter names
- **Operation Failures**: Wraps underlying exceptions in [`InvalidOperationException`](https://docs.microsoft.com/en-us/dotnet/api/system.invalidoperationexception) with detailed error messages
- **Embedding Validation**: Ensures embedding vectors are valid and non-empty before conversion
- **Graceful Degradation**: Handles provider-specific errors and provides meaningful feedback

## Threading and Cancellation

The adapter is designed for high-performance async operations:

- **Full Async Support**: All methods are properly async with [`ConfigureAwait(false)`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.configureawait) for optimal performance
- **Cancellation Token Support**: Supports [`CancellationToken`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken) for long-running operations and graceful shutdown
- **Thread Safety**: Safe for concurrent use across multiple threads (inherits thread safety characteristics from the underlying generator)
- **Resource Management**: Properly manages resources and disposes of them when appropriate

## Dependencies

This package has the following dependencies:

- **Microsoft.Extensions.AI.Abstractions** (>= 9.7.0) - Core AI abstractions from Microsoft
- **AiGeekSquad.AIContext** - Main semantic chunking library (included as project reference)
- **MathNet.Numerics** - Mathematical operations and vector handling (transitively via AIContext)

## Performance Considerations

- **Batch Processing**: Use [`GenerateBatchEmbeddingsAsync()`](../AiGeekSquad.AIContext.MEAI/MicrosoftExtensionsAiEmbeddingGenerator.cs) for multiple texts to leverage provider optimizations
- **Memory Efficiency**: The adapter minimizes memory allocations during vector conversions
- **Async Patterns**: Designed to work efficiently with async/await patterns and high-concurrency scenarios

## Contributing

This package is part of the AiGeekSquad.AIContext project. We welcome contributions!

- **Issues**: Report bugs or request features in the main repository
- **Pull Requests**: Submit improvements following the project's coding standards
- **Documentation**: Help improve documentation and examples

## License

This project is licensed under the MIT License - see the main project repository for full license details.