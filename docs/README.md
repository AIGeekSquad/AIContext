# AiGeekSquad.AIContext Documentation

Welcome to the comprehensive documentation for AiGeekSquad.AIContext - a powerful C# library for AI-powered context management.

## 📚 Documentation Structure

### Getting Started
- **[Main README](../README.md)** - Overview, installation, and quick start examples
- **[Examples](../examples/)** - Working code examples you can run immediately

### Core Features

#### 🧠 Semantic Text Chunking
- **[Semantic Chunking Guide](SemanticChunking.md)** - Complete guide to intelligent text splitting
  - Architecture and components
  - Configuration options and best practices
  - Custom text splitters and embedding providers
  - Performance optimization and troubleshooting

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
- **Token Counting**: [MLTokenCounter](SemanticChunking.md#configuration-options)
- **Similarity Calculation**: [MathNetSimilarityCalculator](MMR.md#performance-considerations)
- **Embedding Generation**: [IEmbeddingGenerator](SemanticChunking.md#implementing-iembeddinggenerator)

## 🛠️ Implementation Guides

### Architecture Patterns
1. **[Dependency Injection Setup](SemanticChunking.md#architecture)**
2. **[Custom Provider Implementation](SemanticChunking.md#custom-text-splitter)**
3. **[Performance Optimization](SemanticChunking.md#performance-optimization)**
4. **[Error Handling Strategies](SemanticChunking.md#error-handling-and-fallbacks)**

### Integration Examples
- **OpenAI Integration**: [Embedding Provider](SemanticChunking.md#openai-example)
- **Azure Cognitive Services**: [Service Integration](SemanticChunking.md#azure-cognitive-services-example)
- **Vector Databases**: [Pinecone Example](SemanticChunking.md#with-vector-databases)
- **Search Platforms**: [Azure Cognitive Search](SemanticChunking.md#with-azure-cognitive-search)

## 📊 Performance & Benchmarks

### Semantic Chunking Performance
- **Streaming Processing**: Memory-efficient with `IAsyncEnumerable`
- **Caching Strategy**: LRU cache for embeddings
- **Token Accuracy**: Real tokenizer implementation

### MMR Performance
- **Benchmark Results**: [Detailed Performance Data](MMR.md#benchmark-results)
- **Optimization Guidelines**: [Performance Tips](MMR.md#optimization-tips)
- **Scalability**: [Large Dataset Handling](MMR.md#performance-guidelines)

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

- **60 Unit Tests** covering all functionality
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