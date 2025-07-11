# AiGeekSquad.AIContext

[![Build status](https://ci.appveyor.com/api/projects/status/1xihiiexyrymgxpg?svg=true)](https://ci.appveyor.com/project/colombod/aicontext)
[![NuGet Version](https://img.shields.io/nuget/v/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive C# library for **AI-powered context management**, providing intelligent text processing capabilities for modern AI applications. This library combines **semantic text chunking** and **Maximum Marginal Relevance (MMR)** algorithms to help you build better RAG systems, search engines, and content recommendation platforms.

## ✨ Features

### 🧠 **Semantic Text Chunking**
- **Intelligent text splitting** based on semantic similarity analysis
- **Configurable chunk sizes** with token-aware boundaries
- **Multiple text splitters** (sentence, custom regex patterns)
- **Embedding-based analysis** using your choice of embedding providers
- **Fallback mechanisms** ensuring robust chunk generation

### 🎯 **Maximum Marginal Relevance (MMR)**
- **High-performance implementation** of the MMR algorithm
- **Relevance-diversity balance** for better search results
- **Optimized for large datasets** with O(n²k) complexity
- **Comprehensive benchmarks** with real performance data

### 🛠️ **Extensible Architecture**
- **Dependency injection ready** with clean interfaces
- **Custom text splitters** for domain-specific requirements
- **Pluggable embedding generators** for different AI models
- **Token counting** with real tokenizer implementations

## 🚀 Quick Start

### Installation

```bash
dotnet add package AiGeekSquad.AIContext
```

### Semantic Text Chunking

```csharp
using AiGeekSquad.AIContext.Chunking;

// Create a chunker with your embedding provider
var tokenCounter = new MLTokenCounter(); // Real GPT-4 compatible tokenizer
var embeddingGenerator = new YourEmbeddingProvider(); // Implement IEmbeddingGenerator

var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);

// Configure chunking options
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 512,
    MinTokensPerChunk = 10,
    BreakpointPercentileThreshold = 0.75 // 75th percentile for breakpoints
};

// Chunk your text with metadata
var text = "Your long document text here...";
var metadata = new Dictionary<string, object>
{
    ["DocumentId"] = "doc-123",
    ["Source"] = "knowledge-base"
};

await foreach (var chunk in chunker.ChunkDocumentAsync(text, metadata, options))
{
    Console.WriteLine($"Chunk: {chunk.Text}");
    Console.WriteLine($"Tokens: {chunk.Metadata["TokenCount"]}");
    Console.WriteLine($"Segments: {chunk.Metadata["SegmentCount"]}");
}
```

### Maximum Marginal Relevance

```csharp
using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext.Ranking;

// Your document embeddings and query
var documents = new List<Vector<double>>
{
    Vector<double>.Build.DenseOfArray(new double[] { 0.8, 0.2, 0.1 }),
    Vector<double>.Build.DenseOfArray(new double[] { 0.7, 0.3, 0.2 }),
    Vector<double>.Build.DenseOfArray(new double[] { 0.1, 0.8, 0.3 })
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

## 📋 Use Cases

### 🔍 **RAG Systems (Retrieval Augmented Generation)**
```csharp
// Chunk documents for vector storage
var chunks = await chunker.ChunkDocumentAsync(document, metadata);

// Later: retrieve and diversify context for LLM
var contextChunks = MaximumMarginalRelevance.ComputeMMR(
    vectors: candidateEmbeddings,
    query: queryEmbedding,
    lambda: 0.8,  // Prioritize relevance for accuracy
    topK: 5
);
```

### 📚 **Document Processing**
- **Knowledge base chunking** for semantic search
- **Legal document analysis** with custom text splitters
- **Research paper processing** with academic content patterns
- **Technical documentation** with code-aware splitting

### 🎯 **Content Recommendation**
- **Diverse article recommendations** using MMR
- **Product recommendation systems** with balanced results
- **Content curation** avoiding redundant information

## 🔧 Configuration Options

### Semantic Chunking Options

| Option | Default | Description |
|--------|---------|-------------|
| `MaxTokensPerChunk` | 512 | Maximum tokens per chunk |
| `MinTokensPerChunk` | 10 | Minimum tokens per chunk |
| `BreakpointPercentileThreshold` | 0.75 | Semantic breakpoint sensitivity (0.0-1.0) |
| `BufferSize` | 1 | Context window for embedding generation |
| `EnableEmbeddingCaching` | true | Cache embeddings for performance |

### Custom Text Splitters

```csharp
// Use custom patterns for domain-specific splitting
var customSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=\d+\.)"); // Numbered lists
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator, customSplitter);

// Or create your own ITextSplitter implementation
public class CodeAwareTextSplitter : ITextSplitter
{
    public async IAsyncEnumerable<TextSegment> SplitAsync(string text, CancellationToken cancellationToken)
    {
        // Your custom splitting logic
    }
}
```

## 📊 Performance

### Semantic Chunking
- **Streaming processing** with `IAsyncEnumerable` for large documents
- **Memory efficient** with configurable embedding cache
- **Token-aware** using real tokenizers (Microsoft.ML.Tokenizers)

### MMR Performance
- **1,000 vectors**: ~2ms processing time
- **Low memory allocation**: ~120KB per 1,000 vectors
- **Optimized for .NET 9.0** with AVX-512 support

See [detailed MMR benchmarks](docs/MMR.md) for comprehensive performance analysis.

## 🏗️ Architecture

### Core Interfaces

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

### Built-in Implementations

- **`MLTokenCounter`**: GPT-4 compatible tokenizer using Microsoft.ML.Tokenizers
- **`SentenceTextSplitter`**: Regex-based sentence splitting with customizable patterns
- **`MathNetSimilarityCalculator`**: Cosine similarity using MathNet.Numerics
- **`EmbeddingCache`**: LRU cache for embedding storage

## 📖 Documentation

- **[MMR Algorithm](docs/MMR.md)**: Detailed MMR documentation with benchmarks
- **[API Reference](https://github.com/AiGeekSquad/AIContext/wiki/API-Reference)**: Complete API documentation
- **[Examples](https://github.com/AiGeekSquad/AIContext/tree/main/examples)**: Sample implementations and use cases

## 🧪 Testing

The library includes comprehensive test coverage:
- **44 unit tests** covering all core functionality
- **Real implementation testing** (no mocks for core algorithms)
- **Edge case handling** with robust fallback mechanisms
- **Performance testing** with benchmarks

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "SemanticChunkingTests"
dotnet test --filter "SentenceTextSplitterTests"
```

## 📦 Dependencies

- **MathNet.Numerics** (v5.0.0): Vector operations and similarity calculations
- **Microsoft.ML.Tokenizers** (v0.22.0): Real tokenization for accurate token counting
- **.NET 9.0**: Target framework for optimal performance

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone and setup
git clone https://github.com/AiGeekSquad/AIContext.git
cd AIContext
dotnet restore
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Support

- **Issues**: [GitHub Issues](https://github.com/AiGeekSquad/AIContext/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AiGeekSquad/AIContext/discussions)
- **Documentation**: [Wiki](https://github.com/AiGeekSquad/AIContext/wiki)

## 🙏 Acknowledgments

- **Carbonell, J. and Goldstein, J. (1998)** - Original MMR algorithm
- **Microsoft** - ML.NET tokenizers for accurate token counting
- **MathNet.Numerics** - Excellent numerical computing library
- **Community contributors** - Thank you for your feedback and contributions

---

**Built with ❤️ for the AI community by AiGeekSquad**
