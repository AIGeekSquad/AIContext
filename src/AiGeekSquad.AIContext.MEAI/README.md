# AiGeekSquad.AIContext.MEAI

A Microsoft Extensions AI Abstractions adapter for the AiGeekSquad.AIContext semantic chunking library.

## Overview

This package provides seamless integration between Microsoft's AI abstractions (`Microsoft.Extensions.AI.Abstractions`) and the AiGeekSquad.AIContext library. It allows you to use any embedding generator that implements Microsoft's `IEmbeddingGenerator<TInput,TEmbedding>` interface with the AIContext semantic chunking functionality.

## Purpose

The `MicrosoftExtensionsAIEmbeddingGenerator` class acts as an adapter that:

- Implements the `AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator` interface
- Wraps any Microsoft Extensions AI embedding generator
- Converts between Microsoft's `Embedding<float>` format and Math.NET's `Vector<double>` format
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

// Your Microsoft Extensions AI embedding generator
IEmbeddingGenerator<string, Embedding<float>> microsoftEmbeddingGenerator = 
    // ... initialize your Microsoft AI embedding generator

// Wrap it with the adapter
IEmbeddingGenerator aiContextEmbeddingGenerator = 
    new MicrosoftExtensionsAIEmbeddingGenerator(microsoftEmbeddingGenerator);

// Use with AIContext semantic chunking
var chunker = new SemanticTextChunker(
    embeddingGenerator: aiContextEmbeddingGenerator,
    // ... other parameters
);

var chunks = await chunker.ChunkTextAsync("Your text to chunk...");
```

### With Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using AiGeekSquad.AIContext.MEAI;
using AiGeekSquad.AIContext.Chunking;

var services = new ServiceCollection();

// Register your Microsoft Extensions AI embedding generator
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    // ... your embedding generator implementation
);

// Register the adapter
services.AddSingleton<IEmbeddingGenerator, MicrosoftExtensionsAIEmbeddingGenerator>();

// Register semantic chunker
services.AddSingleton<SemanticTextChunker>();

var serviceProvider = services.BuildServiceProvider();
var chunker = serviceProvider.GetRequiredService<SemanticTextChunker>();
```

### Advanced Example with Options

```csharp
using AiGeekSquad.AIContext.MEAI;
using AiGeekSquad.AIContext.Chunking;

// Create embedding generator with custom options
var embeddingGenerator = new MicrosoftExtensionsAIEmbeddingGenerator(microsoftGenerator);

// Configure chunking options
var chunkOptions = new ChunkOptions
{
    MaxChunkSize = 1000,
    OverlapSize = 100,
    SimilarityThreshold = 0.7
};

// Create semantic chunker
var chunker = new SemanticTextChunker(
    embeddingGenerator: embeddingGenerator,
    options: chunkOptions
);

// Chunk your text
var text = "Your long document text here...";
var chunks = await chunker.ChunkTextAsync(text);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk: {chunk.Text}");
    Console.WriteLine($"Embedding: [{string.Join(", ", chunk.Embedding.Take(5))}...]");
}
```

## Integration Benefits

By using this adapter, you can:

1. **Leverage Microsoft's AI Ecosystem**: Use any embedding generator that follows Microsoft's AI abstractions
2. **Maintain Compatibility**: Keep your existing AIContext semantic chunking code unchanged
3. **Future-Proof**: Benefit from updates to both Microsoft's AI abstractions and AIContext libraries
4. **Performance**: Take advantage of Microsoft's optimized embedding implementations
5. **Flexibility**: Switch between different embedding providers without changing your chunking logic

## Supported Operations

The adapter supports both single and batch embedding generation:

- **Single Embedding**: `GenerateEmbeddingAsync(string text)`
- **Batch Embeddings**: `GenerateBatchEmbeddingsAsync(IEnumerable<string> texts)`

Both methods convert from Microsoft's `Embedding<float>` format to Math.NET's `Vector<double>` format as required by the AIContext library.

## Error Handling

The adapter provides comprehensive error handling:

- **Null Argument Validation**: Throws `ArgumentNullException` for null inputs
- **Operation Failures**: Wraps underlying exceptions in `InvalidOperationException` with descriptive messages
- **Embedding Validation**: Ensures embedding vectors are valid before conversion

## Threading and Cancellation

The adapter fully supports:

- **Async Operations**: All methods are properly async
- **Cancellation Tokens**: Supports cancellation for long-running operations
- **Thread Safety**: Safe for concurrent use (inherits from underlying generator's thread safety)

## Dependencies

- `Microsoft.Extensions.AI.Abstractions` (>= 9.7.0)
- `AiGeekSquad.AIContext` (included as project reference)
- `MathNet.Numerics` (transitively via AIContext)

## Contributing

This package is part of the AiGeekSquad.AIContext project. For contributions, issues, or feature requests, please visit the main repository.

## License

MIT License - see the main project for full license details.