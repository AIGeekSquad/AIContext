# AiGeekSquad.AIContext Documentation

Welcome to the comprehensive documentation for AiGeekSquad.AIContext - a powerful C# library for AI-powered context management.

## 📖 Documentation

- **[Main README](../README.md)** - Overview, installation, and quick start examples
- **[Examples](../examples/)** - Working code examples you can run immediately
- **[Performance Tuning](PERFORMANCE_TUNING.md)** - Optimization guide for production deployments
- **[Troubleshooting](TROUBLESHOOTING.md)** - Common issues and solutions

### Core Features

#### 🧠 Semantic Text Chunking
- **[Semantic Chunking Guide](SemanticChunking.md)** - Complete guide to intelligent text splitting
  - Architecture and components
  - Configuration options and best practices
  - Custom text splitters and embedding providers
  - Performance optimization and troubleshooting

#### 🔢 Token Counting
- **[MLTokenCounter Documentation](SemanticChunking.md#token-counting)** - Comprehensive token counting capabilities
  - Microsoft.ML.Tokenizers integration for accurate counting
  - Multiple OpenAI model support (GPT-4, GPT-3.5-turbo, embedding models)
  - Model-specific factory methods and alignment guidance
  - Async/sync APIs with comprehensive error handling

#### 🎯 Maximum Marginal Relevance (MMR)
- **[MMR Algorithm Documentation](MMR.md)** - Detailed MMR implementation guide
  - Mathematical foundation and theory
  - Performance benchmarks and optimization
  - API reference and parameter tuning
  - Use cases and integration patterns

## 🚀 Quick Navigation

### By Use Case
- **RAG Systems**: [Semantic Chunking](SemanticChunking.md#rag-system-preparation) + [MMR Context Selection](MMR.md#rag-system-context-selection)
- **Search & Recommendation**: [MMR Diversification](MMR.md#recommendation-system)
- **Document Processing**: [Custom Text Splitters](SemanticChunking.md#text-splitters)
- **Content Analysis**: [Embedding Integration](SemanticChunking.md#implementing-iembeddinggenerator)

### By Component
- **Text Splitting**: [ITextSplitter Interface](SemanticChunking.md#text-splitters)
- **Token Counting**: [MLTokenCounter](SemanticChunking.md#token-counting)
- **Similarity Calculation**: [MathNetSimilarityCalculator](MMR.md#performance-considerations)
- **Embedding Generation**: [IEmbeddingGenerator](SemanticChunking.md#implementing-iembeddinggenerator)

## 🛠️ Implementation Guides

### Architecture Patterns
1. **[Dependency Injection Setup](SemanticChunking.md#architecture)**
2. **[Custom Provider Implementation](SemanticChunking.md#custom-text-splitter)**
3. **[Performance Optimization](SemanticChunking.md#performance-optimization)**
4. **[Error Handling Strategies](SemanticChunking.md#error-handling-and-fallbacks)**

### Token Counting Best Practices
- **[Model Alignment](SemanticChunking.md#token-counter-alignment-with-embedding-models)** - Critical: Align token counter with embedding model for accuracy
- **[Factory Methods](SemanticChunking.md#factory-methods)** - Use pre-configured methods like `CreateGpt4()` or `CreateTextEmbedding3Small()`
- **[Error Handling](SemanticChunking.md#error-handling)** - Comprehensive validation and fallback strategies
- **[Performance](SemanticChunking.md#usage-examples)** - Leverage async APIs for better throughput

### Integration Examples
- **OpenAI Integration**: [Embedding Provider](SemanticChunking.md#openai-example)
- **Azure Cognitive Services**: [Service Integration](SemanticChunking.md#azure-cognitive-services-example)
- **Vector Databases**: [Pinecone Example](SemanticChunking.md#with-vector-databases)
- **Search Platforms**: [Azure Cognitive Search](SemanticChunking.md#with-azure-cognitive-search)

## 📊 Performance & Benchmarks

### Comprehensive Benchmark Results

**Total Benchmarks Executed**: 306 benchmarks across all components
**Runtime**: 8+ hours of comprehensive testing on .NET 9.0
**Platform**: Windows 11, x64 with Server/Workstation GC configurations

### MMR Algorithm Performance
- **1,000 vectors, 100 dimensions**: ~2.1-2.3ms execution time
- **Linear scaling**: 100 vectors (~0.2ms) to 5,000 vectors (~50ms)
- **Lambda impact**: <5% performance variation across relevance/diversity settings
- **Memory efficiency**: Predictable allocation patterns, minimal GC pressure
- **[Detailed MMR Benchmarks](MMR.md#benchmark-results)**

### Ranking Engine Performance
- **Small datasets (100 items)**: 16-25μs for single functions, 40-60μs for multiple
- **Medium datasets (10,000 items)**: 200-550μs depending on complexity
- **Large datasets (100,000 items)**: 1.3-4.2ms with advanced strategies
- **Strategy comparison**: WeightedSum fastest, RRF comparable, Hybrid 10-20% slower
- **Normalization overhead**: MinMax fastest, Percentile 2-3x slower
- **[Detailed Ranking Benchmarks](RankingAPI_Usage.md#benchmark-results)**

### Semantic Chunking Performance
- **Document size scaling**: Linear performance with segment count
- **Token limits**: 512 tokens/chunk optimal for most use cases
- **Caching benefits**: 40-60% improvement with 70-85% hit rates
- **Buffer size impact**: Size 2 provides balanced context/performance
- **Threshold tuning**: 0.75 balanced, 0.85 conservative, 0.95 strict
- **[Detailed Chunking Benchmarks](SemanticChunking.md#benchmark-results)**

### Key Performance Insights
- **Streaming Processing**: Memory-efficient with `IAsyncEnumerable`
- **Caching Strategy**: LRU cache for embeddings with significant performance gains
- **Token Accuracy**: [MLTokenCounter with Microsoft.ML.Tokenizers](SemanticChunking.md#token-counting) for precise, model-aligned token counting
- **GC Efficiency**: Minimal garbage collection impact across all components
- **Scalability**: Predictable performance scaling from small to large datasets

### Complete Benchmark Documentation
- **[Comprehensive Benchmark Results](BenchmarkResults.md)** - Complete 306-benchmark analysis with detailed performance data, methodology, and recommendations

## 🔧 Configuration Reference

### Semantic Chunking Options
```csharp
var options = new SemanticChunkingOptions
{
    MaxTokensPerChunk = 512,              // Token limit per chunk
    MinTokensPerChunk = 10,               // Minimum viable chunk size
    BreakpointPercentileThreshold = 0.75, // Semantic sensitivity
    BufferSize = 1,                       // Context window size
    EnableEmbeddingCaching = true         // Performance optimization
};
```

### MMR Parameters
```csharp
var results = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents,
    query: queryVector,
    lambda: 0.7,    // Relevance vs diversity balance
    topK: 5         // Number of results
);
```

## 🧪 Testing & Quality

- **217 Unit Tests** covering all functionality
- **Real Implementation Testing** (no mocks for core algorithms)
- **Edge Case Coverage** with robust fallback mechanisms
- **Performance Benchmarks** with reproducible results

## 💡 Best Practices

### Content-Specific Recommendations

| Content Type | Max Tokens | Threshold | Buffer | Lambda (MMR) |
|-------------|------------|-----------|--------|--------------|
| Blog Posts | 512 | 0.75 | 1 | 0.5-0.7 |
| Technical Docs | 768 | 0.7 | 2 | 0.6-0.8 |
| Legal Documents | 1024 | 0.8 | 1 | 0.7-0.9 |
| News Articles | 400 | 0.75 | 1 | 0.4-0.6 |
| Academic Papers | 600 | 0.8 | 2 | 0.6-0.8 |

### Development Workflow
1. **Start with defaults** and measure results
2. **Adjust parameters** based on content characteristics
3. **Monitor chunk quality** in production
4. **Fine-tune** based on user feedback

## 🤝 Contributing

We welcome contributions! Please see:
- **[Contributing Guidelines](../CONTRIBUTING.md)** - How to contribute
- **[Development Setup](../README.md#development-setup)** - Getting started
- **[Testing Guide](../README.md#testing)** - Running tests

## 📞 Support & Community

- **Issues**: [GitHub Issues](https://github.com/AiGeekSquad/AIContext/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AiGeekSquad/AIContext/discussions)
- **Wiki**: [Detailed Documentation](https://github.com/AiGeekSquad/AIContext/wiki)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

**Built with ❤️ for the AI community by AiGeekSquad**