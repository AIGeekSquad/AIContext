# AiGeekSquad.AIContext

[![NuGet Version](https://img.shields.io/nuget/v/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive C# library for **AI-powered context management**, providing intelligent text processing capabilities for modern AI applications. This library combines **semantic text chunking** and **Maximum Marginal Relevance (MMR)** algorithms to help you build better RAG systems, search engines, and content recommendation platforms.

## ✨ Key Features

- **🧠 Semantic Text Chunking**: Intelligent text splitting based on semantic similarity analysis
- **🎯 Maximum Marginal Relevance (MMR)**: High-performance algorithm for relevance-diversity balance
- **🛠️ Extensible Architecture**: Dependency injection ready with clean interfaces
- **📊 High Performance**: Optimized for .NET 9.0 with comprehensive benchmarks

## 🚀 Quick Start

### Installation

```bash
dotnet add package AiGeekSquad.AIContext
```

### Basic Usage

#### Semantic Text Chunking

```csharp
using AiGeekSquad.AIContext.Chunking;

// Create a chunker with your embedding provider
var tokenCounter = new MLTokenCounter();
var embeddingGenerator = new YourEmbeddingProvider(); // Implement IEmbeddingGenerator

var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);

// Configure and chunk your text
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 512,
    MinTokensPerChunk = 10,
    BreakpointPercentileThreshold = 0.75
};

var text = "Your long document text here...";
var metadata = new Dictionary<string, object> { ["DocumentId"] = "doc-123" };

await foreach (var chunk in chunker.ChunkDocumentAsync(text, metadata, options))
{
    Console.WriteLine($"Chunk: {chunk.Text}");
    Console.WriteLine($"Tokens: {chunk.Metadata["TokenCount"]}");
}
```

#### Maximum Marginal Relevance

```csharp
using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext.Ranking;

// Your document embeddings and query
var documents = new List<Vector<double>>
{
    Vector<double>.Build.DenseOfArray(new double[] { 0.8, 0.2, 0.1 }),
    Vector<double>.Build.DenseOfArray(new double[] { 0.7, 0.3, 0.2 })
};

var query = Vector<double>.Build.DenseOfArray(new double[] { 0.9, 0.1, 0.0 });

// Get diverse and relevant results
var results = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents,
    query: query,
    lambda: 0.7,  // Balance relevance vs diversity
    topK: 3
);
```

## 🎯 Common Use Cases

### RAG Systems
```csharp
// Chunk documents for vector storage
var chunks = await chunker.ChunkDocumentAsync(document, metadata);

// Later: retrieve and diversify context for LLM
var contextChunks = MaximumMarginalRelevance.ComputeMMR(
    vectors: candidateEmbeddings,
    query: queryEmbedding,
    lambda: 0.8,
    topK: 5
);
```

### Document Processing
- Knowledge base chunking for semantic search
- Legal document analysis with custom text splitters
- Research paper processing with academic content patterns

### Content Recommendation
- Diverse article recommendations using MMR
- Product recommendation systems with balanced results

## ⚙️ Configuration

### Chunking Options

| Option | Default | Description |
|--------|---------|-------------|
| `MaxTokensPerChunk` | 512 | Maximum tokens per chunk |
| `MinTokensPerChunk` | 10 | Minimum tokens per chunk |
| `BreakpointPercentileThreshold` | 0.75 | Semantic breakpoint sensitivity |
| `BufferSize` | 1 | Context window for embedding generation |
| `EnableEmbeddingCaching` | true | Cache embeddings for performance |

### Custom Text Splitters

```csharp
// Use custom patterns for domain-specific splitting
var customSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=\d+\.)");
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator, customSplitter);
```

## 🏗️ Core Interfaces

Implement these interfaces to integrate with your AI infrastructure:

```csharp
// Implement for your embedding provider
public interface IEmbeddingGenerator
{
    IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default);
}

// Implement for custom text splitting
public interface ITextSplitter
{
    IAsyncEnumerable<TextSegment> SplitAsync(
        string text, 
        CancellationToken cancellationToken = default);
}

// Real token counting
public interface ITokenCounter
{
    Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default);
}
```

## 📊 Performance

- **Semantic Chunking**: Streaming processing with `IAsyncEnumerable` for large documents
- **MMR Algorithm**: ~2ms for 1,000 vectors, ~120KB memory allocation
- **Token Counting**: Real GPT-4 compatible tokenizer using Microsoft.ML.Tokenizers

## 📦 Dependencies

- **MathNet.Numerics** (v5.0.0): Vector operations and similarity calculations
- **Microsoft.ML.Tokenizers** (v0.22.0): Real tokenization for accurate token counting
- **.NET 9.0**: Target framework for optimal performance

## 📖 Additional Resources

- **[Repository](https://github.com/AiGeekSquad/AIContext)**: Source code and development information
- **[MMR Documentation](https://github.com/AiGeekSquad/AIContext/blob/main/docs/MMR.md)**: Detailed MMR algorithm documentation
- **[Examples](https://github.com/AiGeekSquad/AIContext/tree/main/examples)**: Sample implementations and use cases
- **[API Reference](https://github.com/AiGeekSquad/AIContext/wiki/API-Reference)**: Complete API documentation

## 🌟 Support

- **Issues**: [GitHub Issues](https://github.com/AiGeekSquad/AIContext/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AiGeekSquad/AIContext/discussions)
- **Documentation**: [Wiki](https://github.com/AiGeekSquad/AIContext/wiki)

---

**Built with ❤️ for the AI community by AiGeekSquad**
