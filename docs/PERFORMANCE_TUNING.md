# Performance Tuning Guide

## Overview

This guide provides recommendations for optimizing the performance of AiGeekSquad.AIContext components based on comprehensive benchmark results and real-world usage patterns.

## Semantic Chunking Performance Tuning

### Configuration Parameters

#### Chunk Size Optimization

**Recommended Configurations by Use Case:**

| Use Case | Max Tokens | Min Tokens | Threshold | Buffer | Reasoning |
|----------|------------|------------|-----------|--------|-----------|
| **RAG Systems** | 512-768 | 50 | 0.75 | 1-2 | Balance context and retrieval speed |
| **Search Indexing** | 256-512 | 25 | 0.70 | 1 | Faster processing, good granularity |
| **Content Analysis** | 768-1024 | 100 | 0.80 | 2-3 | Preserve context, detailed analysis |
| **Real-time Processing** | 256-384 | 25 | 0.65 | 1 | Minimize latency |
| **Batch Processing** | 1024-1536 | 100 | 0.85 | 3-4 | Maximize throughput |

#### Similarity Threshold Tuning

```csharp
// Conservative (fewer, larger chunks)
var conservativeOptions = new SemanticChunkingOptions
{
    BreakpointPercentileThreshold = 0.90, // High threshold
    MaxTokensPerChunk = 1024,
    BufferSize = 3
};

// Balanced (recommended starting point)
var balancedOptions = new SemanticChunkingOptions
{
    BreakpointPercentileThreshold = 0.75, // Default
    MaxTokensPerChunk = 512,
    BufferSize = 1
};

// Aggressive (more, smaller chunks)
var aggressiveOptions = new SemanticChunkingOptions
{
    BreakpointPercentileThreshold = 0.60, // Low threshold
    MaxTokensPerChunk = 256,
    BufferSize = 1
};
```

### Caching Strategies

#### Embedding Cache Optimization

**Memory vs. Performance Trade-offs:**

```csharp
// High-memory, high-performance
var highPerformanceOptions = new SemanticChunkingOptions
{
    EnableEmbeddingCaching = true,
    EmbeddingCacheSize = 2000, // Large cache
    MaxTokensPerChunk = 512
};

// Balanced memory and performance
var balancedCacheOptions = new SemanticChunkingOptions
{
    EnableEmbeddingCaching = true,
    EmbeddingCacheSize = 1000, // Default
    MaxTokensPerChunk = 512
};

// Low-memory, acceptable performance
var lowMemoryOptions = new SemanticChunkingOptions
{
    EnableEmbeddingCaching = true,
    EmbeddingCacheSize = 250, // Small cache
    MaxTokensPerChunk = 384
};
```

**Cache Hit Rate Optimization:**
- **Document Similarity**: Similar documents benefit from larger caches
- **Batch Processing**: Process related documents together
- **Text Preprocessing**: Normalize text to improve cache hits

### Performance Monitoring

```csharp
public async Task<IAsyncEnumerable<TextChunk>> ChunkWithMetrics(
    string text, 
    SemanticChunkingOptions options)
{
    var stopwatch = Stopwatch.StartNew();
    var chunker = SemanticTextChunker.Create(embeddingGenerator, options);
    
    var chunks = new List<TextChunk>();
    await foreach (var chunk in chunker.ChunkAsync(text))
    {
        chunks.Add(chunk);
    }
    
    stopwatch.Stop();
    
    // Log performance metrics
    Console.WriteLine($"Chunking completed:");
    Console.WriteLine($"  Text length: {text.Length} characters");
    Console.WriteLine($"  Chunks created: {chunks.Count}");
    Console.WriteLine($"  Processing time: {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"  Rate: {text.Length / (double)stopwatch.ElapsedMilliseconds * 1000:F0} chars/sec");
    
    return chunks.ToAsyncEnumerable();
}
```

## MMR Algorithm Performance Tuning

### Vector Optimization

#### Dimension Considerations

**Performance Impact by Dimensions:**

| Dimensions | 1K Vectors | 5K Vectors | 10K Vectors | Recommended Use |
|------------|------------|------------|-------------|-----------------|
| **100** | ~2.1ms | ~10ms | ~25ms | Development, testing |
| **384** | ~2.2ms | ~12ms | ~30ms | Production (OpenAI compatible) |
| **768** | ~2.5ms | ~15ms | ~40ms | High-quality embeddings |
| **1536** | ~3.1ms | ~22ms | ~60ms | Premium embedding models |

```csharp
// Optimize for speed with acceptable quality
var speedOptimized = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents.Take(1000).ToList(), // Limit candidate set
    query: queryVector,
    lambda: 0.7, // Favor relevance for speed
    topK: 5      // Minimal results needed
);

// Optimize for quality with acceptable speed
var qualityOptimized = MaximumMarginalRelevance.ComputeMMR(
    vectors: allDocuments,
    query: queryVector,
    lambda: 0.5, // Balanced approach
    topK: 10     // More diverse results
);
```

### Lambda Parameter Tuning

**Performance Characteristics:**

```csharp
// Pure relevance (fastest)
var relevanceOnly = MaximumMarginalRelevance.ComputeMMR(
    vectors, query, lambda: 1.0, topK: k);

// Balanced (recommended)
var balanced = MaximumMarginalRelevance.ComputeMMR(
    vectors, query, lambda: 0.7, topK: k);

// Pure diversity (slightly slower)
var diversityFocused = MaximumMarginalRelevance.ComputeMMR(
    vectors, query, lambda: 0.3, topK: k);
```

**Lambda Selection Guide:**

| Use Case | Recommended Lambda | Reasoning |
|----------|-------------------|-----------|
| **Search Results** | 0.7-0.8 | Emphasize relevance, some diversity |
| **Recommendations** | 0.4-0.6 | Balance novelty and relevance |
| **Content Curation** | 0.5-0.7 | Avoid redundancy while staying relevant |
| **Research/Discovery** | 0.3-0.5 | Prioritize diversity for broader coverage |

### Batch Processing Optimization

```csharp
public class BatchMMRProcessor
{
    private readonly int _batchSize;
    private readonly double _lambda;
    private readonly int _topK;
    
    public BatchMMRProcessor(int batchSize = 1000, double lambda = 0.7, int topK = 10)
    {
        _batchSize = batchSize;
        _lambda = lambda;
        _topK = topK;
    }
    
    public async Task<List<(int index, Vector<double> embedding)>> ProcessLargeDataset(
        List<Vector<double>> allVectors,
        Vector<double> query)
    {
        var results = new List<(int, Vector<double>)>();
        
        // Process in batches to manage memory
        for (int i = 0; i < allVectors.Count; i += _batchSize)
        {
            var batch = allVectors
                .Skip(i)
                .Take(_batchSize)
                .ToList();
                
            var batchResults = MaximumMarginalRelevance.ComputeMMR(
                batch, query, _lambda, _topK);
                
            // Adjust indices to original dataset
            var adjustedResults = batchResults.Select(r => 
                (index: r.index + i, embedding: r.embedding));
                
            results.AddRange(adjustedResults);
        }
        
        // Final MMR on combined results
        var finalCandidates = results.Select(r => r.embedding).ToList();
        var finalResults = MaximumMarginalRelevance.ComputeMMR(
            finalCandidates, query, _lambda, _topK);
            
        return finalResults.Select(r => results[r.index]).ToList();
    }
}
```

## Ranking Engine Performance Tuning

### Strategy Selection

**Performance Comparison (10K items):**

| Strategy | Execution Time | Memory Usage | Best For |
|----------|---------------|--------------|----------|
| **WeightedSum** | ~200-300μs | Low | Simple scoring, speed critical |
| **ReciprocalRankFusion** | ~400-500μs | Medium | Multiple diverse rankings |
| **Hybrid** | ~500-700μs | Medium-High | Complex multi-criteria scenarios |

```csharp
// Speed-optimized configuration
var speedConfig = new List<WeightedScoringFunction<Document>>
{
    new(new RelevanceScorer(), 0.8) { Normalizer = new MinMaxNormalizer() },
    new(new PopularityScorer(), 0.2) { Normalizer = new MinMaxNormalizer() }
};
var results = engine.Rank(documents, speedConfig, new WeightedSumStrategy());

// Quality-optimized configuration
var qualityConfig = new List<WeightedScoringFunction<Document>>
{
    new(new RelevanceScorer(), 0.4) { Normalizer = new ZScoreNormalizer() },
    new(new PopularityScorer(), 0.3) { Normalizer = new PercentileNormalizer() },
    new(new RecencyScorer(), 0.3) { Normalizer = new MinMaxNormalizer() }
};
var results = engine.Rank(documents, qualityConfig, new HybridStrategy());
```

## Context Rendering Performance Tuning

The [`ContextRenderer`](ContextRendering.md) class combines semantic chunking, embedding generation, and MMR ranking with time-based weighting. Performance optimization requires careful consideration of all these components.

### Configuration Optimization

**Performance-Oriented Configuration:**

```csharp
using AiGeekSquad.AIContext.ContextRendering;

// High-performance configuration for real-time chat applications
var tokenCounter = MLTokenCounter.CreateTextEmbedding3Small(); // Fast, accurate tokenization
var renderer = new ContextRenderer(tokenCounter, embeddingGenerator);

// Optimize for speed with acceptable context quality
var context = await renderer.RenderContextAsync(
    query: userQuery,
    maxTokens: 1500,        // Smaller context for faster processing
    relevanceWeight: 0.8,   // Favor relevance over diversity for speed
    freshnessWeight: 0.2    // Minimal time weighting computation
);
```

**Quality-Oriented Configuration:**

```csharp
// Quality-focused configuration for comprehensive RAG systems
var context = await renderer.RenderContextAsync(
    query: userQuery,
    maxTokens: 3000,        // Larger context for better quality
    relevanceWeight: 0.6,   // Balanced relevance and diversity
    freshnessWeight: 0.4    // Significant time weighting for recency
);
```

### Memory and Resource Management

**Conversation Length Optimization:**

| Conversation Length | Max Tokens | Recommended Settings | Performance Impact |
|-------------------|------------|---------------------|-------------------|
| **Short (<10 messages)** | 1000-2000 | relevanceWeight: 0.8, freshnessWeight: 0.2 | Minimal overhead |
| **Medium (10-50 messages)** | 2000-3000 | relevanceWeight: 0.7, freshnessWeight: 0.3 | Moderate chunking cost |
| **Long (50-200 messages)** | 2500-4000 | relevanceWeight: 0.6, freshnessWeight: 0.4 | Significant MMR computation |
| **Very Long (>200 messages)** | 3000-5000 | Consider message pruning before rendering | High computational cost |

**Memory Management for Large Conversations:**

```csharp
public class OptimizedContextService
{
    private readonly ContextRenderer _renderer;
    private readonly int _maxConversationLength;
    
    public OptimizedContextService(ContextRenderer renderer, int maxConversationLength = 100)
    {
        _renderer = renderer;
        _maxConversationLength = maxConversationLength;
    }
    
    public async Task AddMessageAsync(ChatMessage message)
    {
        await _renderer.AddMessageAsync(message);
        
        // Prune old messages to maintain performance
        if (_renderer.MessageCount > _maxConversationLength)
        {
            await _renderer.PruneOldestMessagesAsync(_maxConversationLength / 2);
        }
    }
}
```

### Time-Based Performance Characteristics

**Freshness Weight Impact on Performance:**

```csharp
// Benchmark different freshness weight configurations
public class FreshnessWeightBenchmarks
{
    [Benchmark]
    public async Task<string> NoFreshnessWeight()
    {
        return await renderer.RenderContextAsync(query, 2000, 0.7, 0.0);
    }
    
    [Benchmark]
    public async Task<string> LowFreshnessWeight()
    {
        return await renderer.RenderContextAsync(query, 2000, 0.7, 0.2);
    }
    
    [Benchmark]
    public async Task<string> HighFreshnessWeight()
    {
        return await renderer.RenderContextAsync(query, 2000, 0.7, 0.5);
    }
}
```

**Expected Performance Impact:**
- **No freshness weight (0.0)**: Baseline performance, pure MMR computation
- **Low freshness weight (0.1-0.2)**: ~5-10% performance overhead for time calculations
- **Medium freshness weight (0.3-0.4)**: ~10-15% performance overhead
- **High freshness weight (0.5+)**: ~15-25% performance overhead

### TimeProvider Performance Considerations

**Virtual Time Testing Benefits:**

```csharp
// Production: Uses real time, cannot be controlled
var productionRenderer = new ContextRenderer(tokenCounter, embeddingGenerator);

// Testing: Uses FakeTimeProvider - 10-40x faster tests
var fakeTimeProvider = new FakeTimeProvider();
var testRenderer = new ContextRenderer(tokenCounter, embeddingGenerator, fakeTimeProvider);

// Fast test execution with controlled time
fakeTimeProvider.Advance(TimeSpan.FromMinutes(5)); // Instant time advancement
var context = await testRenderer.RenderContextAsync(query, 2000, 0.7, 0.3);
```

**TimeProvider Performance Guidelines:**
- **Production**: Always use `TimeProvider.System` (null parameter) for real-time behavior
- **Development/Testing**: Use `FakeTimeProvider` for fast, deterministic tests
- **Avoid**: `DateTime.UtcNow` directly - not optimizable and prevents time virtualization

### Normalization Strategy Selection

**Performance vs. Quality Trade-offs:**

```csharp
// Fastest normalization
new WeightedScoringFunction<T>(scorer, weight) 
{ 
    Normalizer = new MinMaxNormalizer() // ~50% faster than alternatives
};

// Balanced normalization  
new WeightedScoringFunction<T>(scorer, weight) 
{ 
    Normalizer = new ZScoreNormalizer() // Good for normally distributed scores
};

// Robust normalization (slower but handles outliers)
new WeightedScoringFunction<T>(scorer, weight) 
{ 
    Normalizer = new PercentileNormalizer() // Best for skewed distributions
};
```

### Top-K Optimization

```csharp
// When you only need top results, use TopK methods
var topResults = engine.RankTopK(
    documents, 
    scoringFunctions, 
    strategy, 
    k: 10); // Much faster than full ranking + Take(10)

// For pagination, consider batch processing
public async Task<PaginatedResults> GetPaginatedResults(
    int page, int pageSize, int totalNeeded = 100)
{
    // Only rank what you need for several pages
    var rankingSize = Math.Min(totalNeeded, documents.Count);
    var ranked = engine.RankTopK(documents, functions, strategy, rankingSize);
    
    return new PaginatedResults
    {
        Items = ranked.Skip(page * pageSize).Take(pageSize),
        TotalRanked = rankingSize
    };
}
```

## System-Level Optimizations

### Memory Management

```csharp
// Configure GC for better performance
public static class PerformanceConfig
{
    public static void OptimizeForThroughput()
    {
        // Use server GC for better throughput
        GCSettings.LatencyMode = GCLatencyMode.Batch;
    }
    
    public static void OptimizeForLatency()
    {
        // Use interactive GC for lower latency
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
    }
}
```

### Parallel Processing

```csharp
// Parallel document processing
public async Task<List<TextChunk>> ProcessDocumentsInParallel(
    IEnumerable<string> documents,
    SemanticChunkingOptions options)
{
    var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    var tasks = documents.Select(async doc =>
    {
        await semaphore.WaitAsync();
        try
        {
            var chunker = SemanticTextChunker.Create(embeddingGenerator, options);
            var chunks = new List<TextChunk>();
            await foreach (var chunk in chunker.ChunkAsync(doc))
            {
                chunks.Add(chunk);
            }
            return chunks;
        }
        finally
        {
            semaphore.Release();
        }
    });
    
    var results = await Task.WhenAll(tasks);
    return results.SelectMany(chunks => chunks).ToList();
}
```

## Monitoring and Profiling

### Performance Metrics Collection

```csharp
public class PerformanceMetrics
{
    public static async Task<T> MeasureAsync<T>(
        Func<Task<T>> operation, 
        string operationName)
    {
        var stopwatch = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            var result = await operation();
            return result;
        }
        finally
        {
            stopwatch.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            
            Console.WriteLine($"Operation: {operationName}");
            Console.WriteLine($"  Duration: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Memory delta: {memoryAfter - memoryBefore} bytes");
            Console.WriteLine($"  Memory after GC: {GC.GetTotalMemory(true)} bytes");
        }
    }
}

// Usage
var chunks = await PerformanceMetrics.MeasureAsync(
    () => ProcessDocument(document),
    "Document Chunking");
```

### Benchmark Integration

```csharp
// Add this to your project for continuous performance monitoring
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ContinuousPerformanceMonitor
{
    private readonly List<Vector<double>> _vectors;
    private readonly Vector<double> _query;
    
    [Benchmark]
    public void MMR_Baseline() =>
        MaximumMarginalRelevance.ComputeMMR(_vectors, _query, 0.7, 10);
        
    [Benchmark] 
    public void Chunking_Baseline() =>
        chunker.ChunkAsync(testDocument).ToListAsync();
}
```

## Environment-Specific Optimizations

### Production Deployment

```csharp
// Production configuration template
public static class ProductionConfig
{
    public static SemanticChunkingOptions GetProductionChunkingOptions()
    {
        return new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BreakpointPercentileThreshold = 0.75,
            BufferSize = 1,
            EnableEmbeddingCaching = true,
            EmbeddingCacheSize = 1000
        };
    }
    
    public static (double lambda, int topK) GetProductionMMRParams(int candidateCount)
    {
        // Adjust parameters based on candidate set size
        return candidateCount switch
        {
            < 1000 => (0.7, Math.Min(20, candidateCount / 10)),
            < 10000 => (0.75, Math.Min(15, candidateCount / 100)),
            _ => (0.8, Math.Min(10, candidateCount / 1000))
        };
    }
}
```

### Development and Testing

```csharp
// Fast configuration for development
public static SemanticChunkingOptions GetDevelopmentOptions()
{
    return new SemanticChunkingOptions
    {
        MaxTokensPerChunk = 256,    // Smaller chunks for faster processing
        BufferSize = 1,             // Minimal context
        BreakpointPercentileThreshold = 0.70, // More aggressive splitting
        EnableEmbeddingCaching = false // Avoid caching during development
    };
}
```

## Conclusion

Performance tuning should be data-driven and use case specific. Start with the recommended configurations above, measure performance in your specific environment, and adjust parameters based on your requirements for speed, memory usage, and result quality.

Remember to:
1. **Profile first**: Measure before optimizing
2. **Test incrementally**: Change one parameter at a time
3. **Validate quality**: Ensure optimizations don't degrade results
4. **Monitor in production**: Set up continuous performance monitoring