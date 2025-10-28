# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AiGeekSquad.AIContext is a comprehensive C# library for AI-powered context management, focusing on semantic text chunking and Maximum Marginal Relevance (MMR) algorithms. It's designed for building RAG systems, search engines, and content recommendation platforms.

## Common Development Commands

### Prerequisites
- **.NET 9.0 SDK** is required (some projects target .NET 9.0, main library targets .NET Standard 2.1)
- Install on Linux/CI environments:
  ```bash
  cd /tmp && wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --version 9.0.303
  export PATH="/home/runner/.dotnet:$PATH"
  ```

### Build and Test
```bash
# Restore dependencies (~9 seconds)
dotnet restore

# Build (Debug: ~9.5s, Release: ~3.7s - use Release for benchmarks)
dotnet build
dotnet build --configuration Release

# Run all tests (~2.8 seconds, 146 tests should pass)
dotnet test

# Run tests with coverage (generates Cobertura and OpenCover formats for SonarQube)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults/ -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura%2Copencover

# Run specific test categories
dotnet test --filter "SemanticChunkingTests"
dotnet test --filter "MaximumMarginalRelevanceTests"
dotnet test --filter "SentenceTextSplitterTests"
```

### Benchmarks (CRITICAL: Set 600+ second timeouts, NEVER cancel)
```bash
# MMR benchmarks (~2 minutes, NEVER CANCEL)
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release mmr

# Semantic chunking benchmarks (~3+ minutes, NEVER CANCEL)
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release semantic

# All benchmarks (10+ minutes, NEVER CANCEL)
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release all

# Export benchmark results
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release -- --exporters json html
```

### Examples and Testing
```bash
# Run examples
dotnet run --project examples/ --configuration Release BasicChunking
dotnet run --project examples/ --configuration Release MMR

# Create test console app
dotnet new console -n MyAIContextTest
cd MyAIContextTest
dotnet add package AiGeekSquad.AIContext
```

## Architecture Overview

### Core Components

The library is built around three main functional areas:

#### 1. Semantic Text Chunking (`src/AiGeekSquad.AIContext/Chunking/`)
- **SemanticTextChunker**: Main chunker using embedding-based semantic analysis
- **Key Interfaces**: `IEmbeddingGenerator`, `ITextSplitter`, `ITokenCounter`, `ISimilarityCalculator`
- **Built-in Implementations**:
  - `SentenceTextSplitter`: Regex-based sentence splitting optimized for English
  - `MLTokenCounter`: GPT-4 compatible tokenizer using Microsoft.ML.Tokenizers
  - `MathNetSimilarityCalculator`: Cosine similarity using MathNet.Numerics
  - `EmbeddingCache`: LRU cache for embedding storage
- **Streaming Architecture**: Uses `IAsyncEnumerable` for memory-efficient processing

#### 2. Maximum Marginal Relevance (`src/AiGeekSquad.AIContext/Ranking/MaximumMarginalRelevance.cs`)
- **Purpose**: Diverse document selection balancing relevance and diversity
- **Algorithm**: O(n²k) complexity, highly optimized implementation
- **Usage**: `MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda, topK)`
- **Lambda Parameter**: Controls relevance (1.0) vs diversity (0.0) trade-off

#### 3. Generic Ranking Engine (`src/AiGeekSquad.AIContext/Ranking/`)
- **RankingEngine**: Multi-criteria ranking with customizable scoring functions
- **Normalizers**: MinMax, ZScore, Percentile normalization strategies
- **Strategies**: WeightedSum, Reciprocal Rank Fusion, Hybrid combination approaches
- **Extensible**: Support for custom scoring functions and strategies

### Project Structure

```
src/
├── AiGeekSquad.AIContext/              # Main library (.NET Standard 2.1)
│   ├── Chunking/                       # Semantic text chunking
│   ├── Ranking/                        # MMR and ranking algorithms
│   └── Properties/
├── AiGeekSquad.AIContext.Tests/        # Unit tests (.NET 9.0)
├── AiGeekSquad.AIContext.Benchmarks/   # Performance benchmarks (.NET 9.0)
└── AiGeekSquad.AIContext.MEAI/         # Microsoft.Extensions.AI integration
```

### Key Dependencies

- **MathNet.Numerics v5.0.0**: Vector operations and similarity calculations
- **Microsoft.ML.Tokenizers v1.0.2**: Real tokenization for accurate token counting
- **Markdig v0.42.0**: Markdown processing
- **.NET Standard 2.1**: Main library target for broad compatibility
- **.NET 9.0**: Test and benchmark projects for latest performance optimizations

## Development Patterns

### Interface-Driven Design
The library uses dependency injection-ready interfaces for all major components. Implement these interfaces for custom providers:

```csharp
// Custom embedding provider
public interface IEmbeddingGenerator
{
    Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}

// Custom text splitting
public interface ITextSplitter
{
    IAsyncEnumerable<TextSegment> SplitAsync(string text, CancellationToken cancellationToken = default);
}
```

### Performance Considerations

- **Always use Release builds for benchmarks and performance testing**
- **Benchmark expectations**:
  - MMR algorithm: ~2ms for 1,000 vectors (384 dimensions)
  - Memory allocation: ~120KB per 1,000 vectors
  - Semantic chunking varies by document size and caching
- **Vector normalization**: Normalize input vectors to unit length for consistent cosine similarity
- **Lambda values**: Use 0.3-0.7 for most practical applications
- **Large datasets**: Consider pre-filtering with approximate similarity search

### Testing Standards

- **217 unit tests** with >90% coverage requirement
- **Real implementation testing** (minimal mocks for core algorithms)
- **Edge case handling** with robust fallback mechanisms
- **Performance benchmarks** for all performance-critical changes
- **Test categories**:
  - `SemanticChunkingTests`: Core chunking logic
  - `SentenceTextSplitterTests`: Text splitting functionality
  - `MaximumMarginalRelevanceTests`: MMR algorithm validation

## Common Use Cases

### RAG Systems
```csharp
// Complete RAG pipeline with MMR for diverse context selection
var contextForLLM = MaximumMarginalRelevance.ComputeMMR(
    vectors: candidates.Select(c => c.Embedding).ToList(),
    query: queryEmbedding,
    lambda: 0.8,  // Prioritize relevance for accuracy
    topK: 5       // Limit context size for LLM
);
```

### Content Recommendation
```csharp
// Diverse recommendations using MMR
var recommendations = MaximumMarginalRelevance.ComputeMMR(
    vectors: itemEmbeddings,
    query: userPreferences,
    lambda: 0.6,  // Favor diversity for better user experience
    topK: 10
);
```

### Multi-Criteria Ranking
```csharp
// Combine multiple scoring functions with different weights and normalizers
var scoringFunctions = new List<WeightedScoringFunction<Document>>
{
    new(new SemanticRelevanceScorer(), weight: 0.7) { Normalizer = new MinMaxNormalizer() },
    new(new PopularityScorer(), weight: -0.3) { Normalizer = new ZScoreNormalizer() }
};
var results = engine.Rank(documents, scoringFunctions, new WeightedSumStrategy());
```

## Solution Configuration

- **Main solution file**: `AiContext.slnx` (use for solution-wide operations)
- **Build entire solution**: `dotnet build AiContext.slnx --configuration Release`
- **CI/CD**: GitHub Actions for SonarQube analysis, AppVeyor builds on Windows
- **NuGet packaging**: `dotnet pack AiContext.slnx --configuration Release --no-build --output packages`

## Important Development Notes

- **Never cancel benchmarks**: They can take 2-10+ minutes and provide critical performance data
- **Always run tests after changes**: Use `dotnet test` to verify all 146 tests pass
- **Use appropriate timeouts**: Build operations need 30-60s, benchmarks need 600+ seconds
- **Follow existing patterns**: The codebase uses consistent naming and architectural patterns
- **Documentation**: Update XML comments for public APIs, maintain comprehensive examples