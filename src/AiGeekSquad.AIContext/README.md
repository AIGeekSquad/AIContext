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

// Configure chunking for your use case
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 512,
    MinTokensPerChunk = 10,
    BreakpointPercentileThreshold = 0.75 // Higher = more semantic breaks
};

// Process a document with metadata
var text = @"
Artificial intelligence is transforming how we work and live. Machine learning
algorithms can process vast amounts of data to find patterns humans might miss.

In the business world, companies adopt AI for customer service, fraud detection,
and process automation. Chatbots handle routine inquiries while algorithms
detect suspicious transactions in real-time.";

var metadata = new Dictionary<string, object>
{
    ["Source"] = "AI Technology Overview",
    ["DocumentId"] = "doc-123"
};

await foreach (var chunk in chunker.ChunkDocumentAsync(text, metadata, options))
{
    Console.WriteLine($"Chunk {chunk.StartIndex}-{chunk.EndIndex}:");
    Console.WriteLine($"  Text: {chunk.Text.Trim()}");
    Console.WriteLine($"  Tokens: {chunk.Metadata["TokenCount"]}");
    Console.WriteLine($"  Segments: {chunk.Metadata["SegmentCount"]}");
    Console.WriteLine();
}
```

#### Maximum Marginal Relevance for Diverse Results

```csharp
using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext.Ranking;

// Simulate document embeddings (from your vector database)
var documents = new List<Vector<double>>
{
    Vector<double>.Build.DenseOfArray(new double[] { 0.9, 0.1, 0.0 }), // ML intro
    Vector<double>.Build.DenseOfArray(new double[] { 0.85, 0.15, 0.0 }), // Advanced ML (similar!)
    Vector<double>.Build.DenseOfArray(new double[] { 0.1, 0.8, 0.1 }), // Sports content
    Vector<double>.Build.DenseOfArray(new double[] { 0.0, 0.1, 0.9 }) // Cooking content
};

var documentTitles = new[]
{
    "Introduction to Machine Learning",
    "Advanced Machine Learning Techniques", // Very similar to first
    "Basketball Training Guide",
    "Italian Cooking Recipes"
};

// User query: interested in machine learning
var query = Vector<double>.Build.DenseOfArray(new double[] { 0.9, 0.1, 0.0 });

// Compare pure relevance vs MMR
Console.WriteLine("Pure Relevance (λ = 1.0):");
var pureRelevance = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents, query: query, lambda: 1.0, topK: 3);

foreach (var (index, score) in pureRelevance)
    Console.WriteLine($"  {documentTitles[index]} (score: {score:F3})");

Console.WriteLine("\nMMR Balanced (λ = 0.7):");
var mmrResults = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents, query: query, lambda: 0.7, topK: 3);

foreach (var (index, score) in mmrResults)
    Console.WriteLine($"  {documentTitles[index]} (score: {score:F3})");

// MMR avoids selecting both similar ML documents!
```

## 🎯 Real-World Examples

### Complete RAG System Pipeline
```csharp
using AiGeekSquad.AIContext.Chunking;
using AiGeekSquad.AIContext.Ranking;

// 1. INDEXING: Chunk documents for vector storage
var documents = new[] { "AI research paper content...", "ML tutorial content..." };
var allChunks = new List<TextChunk>();

foreach (var doc in documents)
{
    await foreach (var chunk in chunker.ChunkDocumentAsync(doc, metadata))
    {
        allChunks.Add(chunk);
        // Store chunk.Text and embedding in your vector database
    }
}

// 2. RETRIEVAL: User asks a question
var userQuestion = "What are the applications of machine learning?";
var queryEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(userQuestion);

// Get candidate chunks from vector database (similarity search)
var candidates = await vectorDb.SearchSimilarAsync(queryEmbedding, topK: 20);

// 3. CONTEXT SELECTION: Use MMR for diverse, relevant context
var selectedContext = MaximumMarginalRelevance.ComputeMMR(
    vectors: candidates.Select(c => c.Embedding).ToList(),
    query: queryEmbedding,
    lambda: 0.8,  // Prioritize relevance but ensure diversity
    topK: 5       // Limit context for LLM token limits
);

// 4. GENERATION: Send to LLM with selected context
var contextText = string.Join("\n\n",
    selectedContext.Select(s => candidates[s.Index].Text));

var prompt = $"Context:\n{contextText}\n\nQuestion: {userQuestion}\nAnswer:";
var response = await llm.GenerateAsync(prompt);
```

### Smart Document Processing
```csharp
// Custom splitter for legal documents
var legalSplitter = SentenceTextSplitter.WithPattern(
    @"(?<=\d+\.)\s+(?=[A-Z])"); // Split on numbered sections

var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator, legalSplitter);

// Process with domain-specific options
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 1024,  // Larger chunks for legal context
    BreakpointPercentileThreshold = 0.8  // More conservative splitting
};

await foreach (var chunk in chunker.ChunkDocumentAsync(legalDocument, metadata, options))
{
    // Each chunk maintains legal context integrity
    await indexService.AddChunkAsync(chunk);
}
```

### Content Recommendation with Diversity
```csharp
// User has read these articles (represented as embeddings)
var userHistory = new List<Vector<double>> { /* user's read articles */ };

// Available articles to recommend
var availableArticles = new List<(string title, Vector<double> embedding)>
{
    ("Machine Learning Basics", mlBasicsEmbedding),
    ("Advanced ML Techniques", advancedMlEmbedding),  // Similar to above
    ("Data Science Career Guide", dataScienceEmbedding),
    ("Python Programming Tips", pythonEmbedding)
};

// User's interests (derived from their history)
var userInterestVector = ComputeUserInterestVector(userHistory);

// Get diverse recommendations (avoid recommending similar content)
var recommendations = MaximumMarginalRelevance.ComputeMMR(
    vectors: availableArticles.Select(a => a.embedding).ToList(),
    query: userInterestVector,
    lambda: 0.6,  // Balance relevance with diversity
    topK: 3
);

foreach (var (index, score) in recommendations)
{
    Console.WriteLine($"Recommended: {availableArticles[index].title}");
}
```

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
