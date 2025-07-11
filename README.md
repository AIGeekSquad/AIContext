# AiGeekSquad.AIContext
[![Build status](https://ci.appveyor.com/api/projects/status/1xihiiexyrymgxpg?svg=true)](https://ci.appveyor.com/project/colombod/aicontext)
[![NuGet Version](https://img.shields.io/nuget/v/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AiGeekSquad.AIContext.svg)](https://www.nuget.org/packages/AiGeekSquad.AIContext/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance C# implementation of the **Maximum Marginal Relevance (MMR)** algorithm for intelligent document selection. This library provides efficient algorithms for balancing relevance and diversity in search results, making it ideal for AI applications, information retrieval systems, and content recommendation engines.

## What is Maximum Marginal Relevance (MMR)?

Maximum Marginal Relevance (MMR) is a re-ranking algorithm designed to reduce redundancy while maintaining query relevance in information retrieval systems. Originally proposed by Carbonell and Goldstein (1998), MMR addresses the fundamental problem of selecting a diverse subset of documents from a larger set of relevant documents.

### Mathematical Foundation

The MMR algorithm works by iteratively selecting documents that maximize a linear combination of two criteria:

- **Relevance**: Similarity to the query vector (measured using cosine similarity)
- **Diversity**: Dissimilarity to already selected documents (promoting variety in results)

The mathematical formulation is:

```
MMR = λ × Sim(Di, Q) - (1-λ) × max(Sim(Di, Dj))
```

Where:
- `Di` is a candidate document
- `Q` is the query vector
- `Dj` represents already selected documents
- `λ` controls the relevance-diversity trade-off (0.0 to 1.0)

### Why MMR is Useful

MMR solves several critical problems in information retrieval:

1. **Redundancy Reduction**: Prevents selecting multiple similar documents
2. **Diversity Enhancement**: Ensures variety in search results
3. **Relevance Preservation**: Maintains connection to the original query
4. **Configurable Balance**: Allows tuning between relevance and diversity

## Installation

Install the package via NuGet Package Manager:

```bash
dotnet add package AiGeekSquad.AIContext
```

Or via Package Manager Console in Visual Studio:

```powershell
Install-Package AiGeekSquad.AIContext
```

Or add directly to your `.csproj` file:

```xml
<PackageReference Include="AiGeekSquad.AIContext" Version="1.0.0" />
```

## Quick Start

```csharp
using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext;

// Create document embeddings (typically from a neural network)
var documents = new List<Vector<double>>
{
    Vector<double>.Build.DenseOfArray(new double[] { 0.8, 0.2, 0.1 }),  // Tech article
    Vector<double>.Build.DenseOfArray(new double[] { 0.7, 0.3, 0.2 }),  // Similar tech article
    Vector<double>.Build.DenseOfArray(new double[] { 0.1, 0.8, 0.3 }),  // Sports article
    Vector<double>.Build.DenseOfArray(new double[] { 0.2, 0.1, 0.9 })   // Arts article
};

// Query vector representing user's interest
var query = Vector<double>.Build.DenseOfArray(new double[] { 0.9, 0.1, 0.0 });

// Select top 3 diverse and relevant documents
var results = MaximumMarginalRelevance.ComputeMMR(
    vectors: documents,
    query: query,
    lambda: 0.7,  // Prefer relevance but include some diversity
    topK: 3
);

// Process results
foreach (var (index, vector) in results)
{
    Console.WriteLine($"Selected document {index} with embedding {vector}");
}
```

## Usage Examples

### 1. Recommendation System

```csharp
// For a recommendation system with user preference vector
var userPreferences = Vector<double>.Build.DenseOfArray(new double[] { 0.6, 0.3, 0.1 });
var itemEmbeddings = GetItemEmbeddings(); // Your method to get item vectors

// Get diverse recommendations
var recommendations = MaximumMarginalRelevance.ComputeMMR(
    vectors: itemEmbeddings,
    query: userPreferences,
    lambda: 0.5,  // Balanced relevance and diversity
    topK: 10      // Top 10 recommendations
);

var recommendedItems = recommendations.Select(r => GetItemById(r.index)).ToList();
```

### 2. RAG System Context Selection

```csharp
// For Retrieval Augmented Generation (RAG) systems
var contextCandidates = await GetSemanticSearchResults(userQuery);
var queryEmbedding = await GetQueryEmbedding(userQuery);

// Select diverse context chunks to avoid redundancy
var contextForLLM = MaximumMarginalRelevance.ComputeMMR(
    vectors: contextCandidates.Select(c => c.Embedding).ToList(),
    query: queryEmbedding,
    lambda: 0.8,  // Prioritize relevance for accuracy
    topK: 5       // Limit context size for LLM
);

var finalContext = contextForLLM
    .Select(result => contextCandidates[result.index].Text)
    .ToList();
```

### 3. Different Lambda Values

```csharp
var vectors = GetDocumentVectors();
var query = GetQueryVector();

// Pure relevance (λ = 1.0) - ignores diversity
var relevantResults = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 1.0, topK: 5);

// Balanced approach (λ = 0.5) - equal weight to relevance and diversity
var balancedResults = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 5);

// Pure diversity (λ = 0.0) - ignores query relevance
var diverseResults = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.0, topK: 5);
```

## Performance Considerations

### Time and Space Complexity

- **Time Complexity**: O(n²k) where n is the number of input vectors and k is topK
- **Space Complexity**: O(n) for similarity caching
- **Query similarities are precomputed once** for efficiency

### Benchmark Results

Based on comprehensive benchmarks using BenchmarkDotNet on .NET 9.0 with AVX-512 optimizations:

**Real Performance Data** (from actual benchmark runs):
- **1,000 vectors, 10 dimensions, topK=5**:
  - Pure Relevance (λ=1.0): **1.87ms** ± 0.01ms
  - Pure Diversity (λ=0.0): **2.07ms** ± 0.05ms
  - Balanced (λ=0.5): **1.89ms** ± 0.04ms
- **Memory allocation**: ~120KB per 1,000 vectors
- **GC pressure**: Minimal (Gen 0/1/2: 0/0/0)

### Performance Guidelines

| Vector Count | Dimensions | Recommended topK | Expected Performance | Memory Usage |
|-------------|------------|------------------|---------------------|--------------|
| < 100       | Any        | Any              | < 0.5ms             | < 10KB       |
| 100-1,000   | < 100      | < 20             | 0.5-5ms             | 10-200KB     |
| 1,000-5,000 | < 500      | < 20             | 2-50ms              | 200KB-2MB    |
| > 5,000     | Any        | < 20             | Consider pre-filtering | > 2MB     |

**Performance Notes**:
- Lambda values have minimal impact on performance (< 10% difference)
- Higher dimensions increase memory usage linearly
- TopK has minimal impact on performance for reasonable values (< 50)

### Benchmark Methodology

The performance data above comes from comprehensive benchmarks using:
- **BenchmarkDotNet v0.13.12** for accurate measurements
- **.NET 9.0** runtime with AVX-512 optimizations
- **Multiple GC configurations** (Workstation and Server GC)
- **Statistical analysis** with confidence intervals
- **Memory diagnostics** tracking allocations and GC pressure
- **Reproducible test data** using fixed seed (42)

Benchmarks test various parameter combinations:
- Vector counts: 100, 1,000, 5,000
- Dimensions: 10, 100, 500
- TopK values: 5, 10, 20
- Lambda values: 0.0, 0.5, 1.0

### Optimization Tips

1. **Pre-filtering**: For large collections (>1000 vectors), consider pre-filtering with approximate similarity search
2. **Vector Normalization**: Normalize input vectors to unit length for consistent cosine similarity behavior
3. **Appropriate topK**: Set reasonable topK values to balance result quality and computational cost
4. **Lambda Tuning**: Use lambda values between 0.3-0.7 for most practical applications

## API Documentation

### ComputeMMR Method

```csharp
public static List<(int index, Vector<double> embedding)> ComputeMMR(
    List<Vector<double>> vectors, 
    Vector<double> query, 
    double lambda = 0.5, 
    int? topK = null)
```

#### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `vectors` | `List<Vector<double>>` | Required | Collection of vectors to select from. All vectors must have the same dimensions as the query vector. |
| `query` | `Vector<double>` | Required | Query vector representing the information need. Must have the same dimensionality as all input vectors. |
| `lambda` | `double` | `0.5` | Controls relevance-diversity trade-off (0.0 to 1.0). Higher values prioritize relevance; lower values prioritize diversity. |
| `topK` | `int?` | `null` | Maximum number of vectors to select. If null, selects all vectors in MMR order. |

#### Returns

`List<(int index, Vector<double> embedding)>` - List of tuples containing:
- `index`: Zero-based index of the vector in the original input collection
- `embedding`: Reference to the original vector object

Results are ordered by selection priority according to the MMR algorithm.

#### Exceptions

| Exception | Condition |
|-----------|-----------|
| `ArgumentNullException` | When query vector is null |
| `ArgumentException` | When lambda is outside [0.0, 1.0] range or vectors have inconsistent dimensions |

### Lambda Parameter Guide

| Lambda Value | Behavior | Use Case |
|-------------|----------|----------|
| `1.0` | Pure relevance - selects vectors most similar to query | Precision-focused search |
| `0.7-0.9` | Relevance-focused with some diversity | Most search applications |
| `0.5` | Balanced approach (recommended default) | General-purpose usage |
| `0.1-0.3` | Diversity-focused with some relevance | Content discovery |
| `0.0` | Pure diversity - ignores query relevance | Maximum variety |

## Best Practices

### Vector Preparation

1. **Normalize vectors** to unit length for consistent cosine similarity behavior
2. **Ensure consistent dimensions** across all vectors and query
3. **Use appropriate vector representations** (embeddings from neural networks, TF-IDF, etc.)

### Parameter Selection

1. **Start with λ = 0.5** for balanced results
2. **Adjust λ based on use case**:
   - Information retrieval: 0.6-0.8
   - Recommendation systems: 0.4-0.6
   - Content discovery: 0.2-0.4
3. **Set reasonable topK values** to balance quality and performance

### Integration Patterns

1. **Combine with approximate search** for large datasets
2. **Cache query embeddings** when processing multiple similar queries
3. **Monitor performance** and adjust parameters based on user feedback

## Common Use Cases

- **Search Result Diversification**: Improve search engines by reducing redundant results
- **Recommendation Systems**: Provide diverse product or content recommendations
- **Document Summarization**: Select diverse sentences or paragraphs for summaries
- **Content Curation**: Avoid redundant information in curated content feeds
- **RAG Systems**: Select diverse context chunks for language model prompts
- **Research Paper Recommendation**: Ensure topical diversity in academic recommendations

## Dependencies

- **MathNet.Numerics** (v5.0.0): Used for vector operations and cosine distance calculations
- **.NET 9.0**: Target framework

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please follow these guidelines:

1. **Fork the repository** and create a feature branch
2. **Write tests** for new functionality
3. **Follow C# coding conventions** and maintain code quality
4. **Update documentation** for API changes
5. **Submit a pull request** with a clear description

### Development Setup

```bash
# Clone the repository
git clone https://github.com/AiGeekSquad/AIContext.git

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests
dotnet test

# Run benchmarks (optional)
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release
```

### Testing

The project includes comprehensive unit tests covering:
- Basic functionality with various lambda values
- Edge cases (empty collections, invalid parameters)
- Performance tests with larger datasets
- Dimension validation and error handling

Run tests with:
```bash
dotnet test --verbosity normal
```

## Benchmarks

Performance benchmarks are available in the `AiGeekSquad.AIContext.Benchmarks` project. These benchmarks test various parameter combinations to help you understand performance characteristics for your specific use case.

See [Benchmarks README](src/AiGeekSquad.AIContext.Benchmarks/README.md) for detailed information.

## Acknowledgments

- **Carbonell, J. and Goldstein, J. (1998)** - Original MMR algorithm paper: "The Use of MMR, Diversity-Based Reranking for Reordering Documents and Producing Summaries"
- **MathNet.Numerics** - Excellent numerical library for .NET
- **Community contributors** - Thank you for your contributions and feedback

## Support

- **Issues**: [GitHub Issues](https://github.com/AiGeekSquad/AIContext/issues)
- **Discussions**: [GitHub Discussions](https://github.com/AiGeekSquad/AIContext/discussions)
- **Documentation**: [Wiki](https://github.com/AiGeekSquad/AIContext/wiki)

---

**Made with ❤️ by AiGeekSquad**
