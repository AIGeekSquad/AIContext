# Semantic Text Chunking

This document provides comprehensive documentation for the semantic text chunking implementation in AiGeekSquad.AIContext.

## Overview

Semantic text chunking is an intelligent approach to splitting large texts into meaningful segments based on semantic similarity rather than simple length-based rules. This approach ensures that related content stays together while maintaining optimal chunk sizes for AI applications.

## Key Concepts

### What is Semantic Chunking?

Traditional text chunking methods split text based on fixed rules like character count, word count, or simple punctuation. Semantic chunking uses **embedding similarity analysis** to identify natural breakpoints where the meaning or topic shifts, resulting in more coherent chunks.

### How It Works

1. **Text Segmentation**: Text is first split into smaller units (sentences, paragraphs, etc.)
2. **Embedding Generation**: Each segment gets converted to a vector embedding
3. **Similarity Analysis**: Adjacent segments are compared for semantic similarity
4. **Breakpoint Detection**: Points with low similarity become chunk boundaries
5. **Token Validation**: Chunks are validated against size constraints
6. **Fallback Logic**: Ensures meaningful chunks are always produced

## Architecture

### Core Components

```csharp
// Main chunking orchestrator
SemanticTextChunker

// Text splitting strategies
ITextSplitter
├── SentenceTextSplitter
└── [Your custom implementations]

// Token counting
ITokenCounter
└── MLTokenCounter

// Embedding generation (implement yourself)
IEmbeddingGenerator

// Similarity calculations
ISimilarityCalculator
└── MathNetSimilarityCalculator
```

## Quick Start

### Basic Usage

```csharp
using AiGeekSquad.AIContext.Chunking;

// Setup dependencies
var tokenCounter = new MLTokenCounter();
var embeddingGenerator = new YourEmbeddingProvider(); // Implement IEmbeddingGenerator

// Create chunker
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);

// Chunk text
var text = "Your document text here...";
await foreach (var chunk in chunker.ChunkAsync(text))
{
    Console.WriteLine($"Chunk: {chunk.Text}");
    Console.WriteLine($"Tokens: {chunk.Metadata["TokenCount"]}");
}
```

### With Custom Options

```csharp
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 1024,        // Larger chunks
    MinTokensPerChunk = 50,          // Minimum viable size
    BreakpointPercentileThreshold = 0.8,  // More sensitive breakpoint detection
    BufferSize = 2,                  // More context for embeddings
    EnableEmbeddingCaching = true    // Cache for performance
};

await foreach (var chunk in chunker.ChunkAsync(text, options))
{
    // Process chunks
}
```

### With Metadata

```csharp
var metadata = new Dictionary<string, object>
{
    ["DocumentId"] = "doc-123",
    ["Source"] = "knowledge-base",
    ["Category"] = "technical",
    ["Author"] = "AI Assistant"
};

await foreach (var chunk in chunker.ChunkDocumentAsync(text, metadata, options))
{
    // Original metadata is preserved and enhanced
    Console.WriteLine($"Document ID: {chunk.Metadata["DocumentId"]}");
    Console.WriteLine($"Token Count: {chunk.Metadata["TokenCount"]}");
    Console.WriteLine($"Segment Count: {chunk.Metadata["SegmentCount"]}");
}
```

## Configuration Options

### SemanticChunkingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxTokensPerChunk` | `int` | 512 | Maximum tokens allowed per chunk |
| `MinTokensPerChunk` | `int` | 10 | Minimum tokens required per chunk |
| `BreakpointPercentileThreshold` | `double` | 0.75 | Percentile threshold for semantic breakpoints (0.0-1.0) |
| `BufferSize` | `int` | 1 | Number of segments before/after for context |
| `EnableEmbeddingCaching` | `bool` | true | Enable caching of embeddings |
| `MaxCacheSize` | `int` | 1000 | Maximum cached embeddings |

### Breakpoint Threshold Guide

| Threshold | Behavior | Use Case |
|-----------|----------|----------|
| 0.95 | Very strict - few breakpoints | Highly coherent topics |
| 0.85 | Strict - conservative chunking | Technical documents |
| 0.75 | Balanced - good for most content | **Default recommendation** |
| 0.65 | Lenient - more breakpoints | Diverse content |
| 0.5 | Very lenient - many breakpoints | Mixed topics |

## Text Splitters

### Built-in SentenceTextSplitter

```csharp
// Default sentence splitting
var splitter = new SentenceTextSplitter();

// Custom regex pattern
var customSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=\d+\.)"); // Numbered lists

// Use with chunker
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator, customSplitter);
```

### Handling Special Content

```csharp
// For technical documentation with code blocks
var codeSplitter = SentenceTextSplitter.WithPattern(@"(?<=```)\s*\n|(?<=\.)\s+(?=[A-Z])");

// For legal documents with numbered sections
var legalSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=\d+\.\d+)");

// For academic papers with citations
var academicSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=[A-Z][^.]*\([12]\d{3}\))");
```

### Custom Text Splitter

```csharp
public class ParagraphTextSplitter : ITextSplitter
{
    public async IAsyncEnumerable<TextSegment> SplitAsync(
        string text, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        int currentIndex = 0;
        
        foreach (var paragraph in paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var trimmed = paragraph.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                var startIndex = text.IndexOf(trimmed, currentIndex);
                var endIndex = startIndex + trimmed.Length;
                
                yield return new TextSegment(trimmed, startIndex, endIndex);
                currentIndex = endIndex;
            }
            
            await Task.Yield(); // Allow cancellation
        }
    }
}
```

## Implementing IEmbeddingGenerator

The library requires you to implement `IEmbeddingGenerator` for your specific embedding provider:

### OpenAI Example

```csharp
public class OpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly OpenAIClient _client;
    
    public OpenAIEmbeddingGenerator(string apiKey)
    {
        _client = new OpenAIClient(apiKey);
    }
    
    public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var response = await _client.EmbeddingsEndpoint.CreateEmbeddingAsync(
            textList, 
            "text-embedding-3-small", 
            cancellationToken: cancellationToken);
            
        foreach (var embedding in response.Data)
        {
            yield return Vector<double>.Build.DenseOfArray(embedding.Embedding.ToArray());
        }
    }
}
```

### Azure Cognitive Services Example

```csharp
public class AzureEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    
    public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var text in texts)
        {
            var embedding = await GetEmbeddingAsync(text, cancellationToken);
            yield return Vector<double>.Build.DenseOfArray(embedding);
        }
    }
    
    private async Task<double[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        // Implementation for Azure Cognitive Services
        // Return embedding array
    }
}
```

## Performance Optimization

### Caching Strategy

```csharp
var options = new SemanticChunkingOptions
{
    EnableEmbeddingCaching = true,  // Enable caching
    MaxCacheSize = 5000,            // Increase cache size for large documents
};

// Cache is automatically managed with LRU eviction
```

### Batch Processing

```csharp
// For multiple documents, reuse the same chunker instance
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);

foreach (var document in documents)
{
    await foreach (var chunk in chunker.ChunkDocumentAsync(document.Text, document.Metadata))
    {
        await ProcessChunkAsync(chunk);
    }
}
```

### Memory Management

```csharp
// Use streaming for large documents
await foreach (var chunk in chunker.ChunkAsync(largeText))
{
    // Process each chunk immediately
    await ProcessChunkAsync(chunk);
    
    // Chunk is eligible for GC after processing
}
```

## Error Handling and Fallbacks

### Automatic Fallbacks

The chunker includes robust fallback mechanisms:

1. **No Semantic Breakpoints**: Falls back to token-based splitting
2. **All Chunks Too Small**: Creates single chunk (if within max tokens)
3. **Text Too Large**: Falls back to sentence-by-sentence chunks
4. **Embedding Failures**: Uses distance-based fallback similarity

### Custom Error Handling

```csharp
public class RobustEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly IEmbeddingGenerator _primary;
    private readonly IEmbeddingGenerator _fallback;
    
    public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var text in texts)
        {
            Vector<double>? embedding = null;
            
            try
            {
                embedding = await GetSingleEmbeddingAsync(_primary, text, cancellationToken);
            }
            catch (Exception ex) when (IsRetriableError(ex))
            {
                // Fallback to secondary provider
                embedding = await GetSingleEmbeddingAsync(_fallback, text, cancellationToken);
            }
            
            if (embedding != null)
                yield return embedding;
        }
    }
}
```

## Common Use Cases

### RAG System Preparation

```csharp
// Chunk documents for vector database storage
var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 512,  // Optimal for most LLMs
    MinTokensPerChunk = 50,   // Ensure meaningful content
    BreakpointPercentileThreshold = 0.75  // Balanced chunking
};

var chunks = new List<TextChunk>();
await foreach (var chunk in chunker.ChunkDocumentAsync(document, metadata, options))
{
    chunks.Add(chunk);
}

// Store in vector database
await vectorStore.AddChunksAsync(chunks);
```

### Legal Document Processing

```csharp
// Custom splitter for legal sections
var legalSplitter = SentenceTextSplitter.WithPattern(@"(?<=\.)\s+(?=\d+\.\d+|\([a-z]\)|\([ivx]+\))");

var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator, legalSplitter);
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 1024,  // Longer chunks for complex legal language
    BreakpointPercentileThreshold = 0.8  // Preserve related legal concepts
};
```

### Technical Documentation

```csharp
// Handle code blocks and technical content
var techSplitter = SentenceTextSplitter.WithPattern(@"```[\s\S]*?```|(?<=\.)\s+(?=[A-Z])");

var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 768,   // Balance between context and size
    BufferSize = 2,            // More context for technical terms
    BreakpointPercentileThreshold = 0.7  // Allow more granular chunking
};
```

## Best Practices

### Choosing Parameters

1. **Token Limits**: Set based on your downstream model requirements
2. **Breakpoint Threshold**: Start with 0.75, adjust based on content coherence needs
3. **Buffer Size**: Increase for technical content, keep low for simple text
4. **Caching**: Always enable for production use

### Content-Specific Tuning

| Content Type | Max Tokens | Min Tokens | Threshold | Buffer Size |
|-------------|------------|------------|-----------|-------------|
| Blog Posts | 512 | 25 | 0.75 | 1 |
| Technical Docs | 768 | 50 | 0.7 | 2 |
| Legal Documents | 1024 | 100 | 0.8 | 1 |
| News Articles | 400 | 20 | 0.75 | 1 |
| Academic Papers | 600 | 75 | 0.8 | 2 |

### Quality Assurance

```csharp
// Monitor chunk quality
await foreach (var chunk in chunker.ChunkAsync(text, options))
{
    var tokenCount = (int)chunk.Metadata["TokenCount"];
    var segmentCount = (int)chunk.Metadata["SegmentCount"];
    
    // Log potential issues
    if (tokenCount < options.MinTokensPerChunk * 0.8)
        logger.LogWarning($"Chunk below minimum threshold: {tokenCount} tokens");
        
    if (chunk.Metadata.ContainsKey("IsFallback"))
        logger.LogInfo("Fallback chunking used");
}
```

## Integration Examples

### With Vector Databases

```csharp
// Pinecone integration example
public async Task IndexDocumentAsync(string documentText, Dictionary<string, object> metadata)
{
    var chunks = new List<(string text, Vector<double> embedding, Dictionary<string, object> metadata)>();
    
    await foreach (var chunk in chunker.ChunkDocumentAsync(documentText, metadata))
    {
        var embedding = await embeddingGenerator.GetSingleEmbeddingAsync(chunk.Text);
        chunks.Add((chunk.Text, embedding, chunk.Metadata));
    }
    
    await pineconeIndex.UpsertAsync(chunks);
}
```

### With Azure Cognitive Search

```csharp
// Azure Cognitive Search integration
public async Task IndexDocumentAsync(Document document)
{
    var searchDocuments = new List<SearchDocument>();
    
    await foreach (var chunk in chunker.ChunkDocumentAsync(document.Content, document.Metadata))
    {
        var searchDoc = new SearchDocument
        {
            ["id"] = $"{document.Id}_{chunk.StartIndex}",
            ["content"] = chunk.Text,
            ["tokens"] = chunk.Metadata["TokenCount"],
            ["source"] = chunk.Metadata["Source"]
        };
        
        searchDocuments.Add(searchDoc);
    }
    
    await searchClient.IndexDocumentsAsync(searchDocuments);
}
```

## Troubleshooting

### Common Issues

**Empty Chunks Returned**
- Check `MinTokensPerChunk` - may be too high
- Verify embedding generator is working
- Check if text is too short

**Chunks Too Large/Small**
- Adjust `BreakpointPercentileThreshold`
- Modify `MaxTokensPerChunk`/`MinTokensPerChunk`
- Consider different text splitter

**Poor Chunk Quality**
- Increase `BufferSize` for more context
- Adjust threshold for your content type
- Verify embedding quality

**Performance Issues**
- Enable caching with `EnableEmbeddingCaching`
- Increase `MaxCacheSize`
- Consider batch processing strategies

### Debug Information

```csharp
// Enable debug metadata
var options = new SemanticChunkingOptions { /* your options */ };

await foreach (var chunk in chunker.ChunkAsync(text, options))
{
    Console.WriteLine($"Chunk {chunk.StartIndex}-{chunk.EndIndex}:");
    Console.WriteLine($"  Tokens: {chunk.Metadata["TokenCount"]}");
    Console.WriteLine($"  Segments: {chunk.Metadata["SegmentCount"]}");
    
    if (chunk.Metadata.ContainsKey("IsFallback"))
        Console.WriteLine("  Used fallback logic");
}