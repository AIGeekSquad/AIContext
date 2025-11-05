# AIContext Library - Copilot Instructions

Always follow these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

**CRITICAL: This file provides comprehensive guidance for GitHub Copilot users. Always test your implementations thoroughly and follow the architectural patterns outlined here to maintain code quality and consistency with the AIContext library standards.**

## Working Effectively

### Prerequisites & Setup
- **Install .NET 9.0 SDK** - Required for all projects. Some projects target net9.0, main library targets netstandard2.1.
- Use this exact command to install .NET 9.0:
  ```bash
  cd /tmp && wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --version 9.0.303
  export PATH="/home/runner/.dotnet:$PATH"
  ```
- Verify installation: `dotnet --version` (should show 9.0.x)

### Bootstrap, Build, and Test Commands
- **Restore dependencies**: `dotnet restore` - takes ~9 seconds
- **Build Debug**: `dotnet build` - takes ~9.5 seconds
- **Build Release**: `dotnet build --configuration Release` - takes ~3.7 seconds. FASTER for benchmarks and testing.
- **Run all tests**: `dotnet test` - takes ~2.8 seconds. 146 tests, all should pass. NEVER CANCEL.

### Benchmarks - NEVER CANCEL
- **CRITICAL**: Benchmarks take 2-10+ minutes. NEVER CANCEL. Set timeout to 600+ seconds.
- **MMR benchmarks**: `dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release mmr` - takes ~2 minutes. NEVER CANCEL.
- **Semantic chunking benchmarks**: `dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release semantic` - takes ~3+ minutes. NEVER CANCEL.
- **Ranking engine benchmarks**: `dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release ranking` - timing varies.
- **All benchmarks**: `dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release all` - takes 10+ minutes. NEVER CANCEL.

### Testing Library Functionality
- Create test console app to validate library:
  ```bash
  cd /tmp && mkdir aicontext-test && cd aicontext-test
  dotnet new console -n TestAIContext
  cd TestAIContext
  dotnet add reference /path/to/AIContext/src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj
  # Add test code and run: dotnet run
  ```

## Validation Scenarios

### ALWAYS Test Core Functionality
After making changes to the library, ALWAYS run:
1. `dotnet build --configuration Release` - Verify no build errors
2. `dotnet test` - Verify all 146 tests pass
3. Create and run a simple test program using MaximumMarginalRelevance.ComputeMMR to verify the library works

### Manual Test Template
Use this code template to validate library works:
```csharp
using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics.LinearAlgebra;

var vectors = new List<Vector<double>>
{
    Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 }),
    Vector<double>.Build.DenseOfArray(new double[] { 0, 1, 0 }),
    Vector<double>.Build.DenseOfArray(new double[] { 0.5, 0.5, 0 })
};
var query = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 });
var results = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 2);
Console.WriteLine($"Found {results.Count} results");
```

### CI/CD Validation
- **Always run tests before committing**: `dotnet test` to ensure CI will pass
- **Check SonarQube requirements**: Build must succeed for GitHub Actions SonarQube analysis
- **AppVeyor builds**: Solution builds on Windows with `dotnet build AiContext.slnx --configuration Release`

## Repository Structure

### Key Projects
- **`src/AiGeekSquad.AIContext/`** - Main library (.NET Standard 2.1)
- **`src/AiGeekSquad.AIContext.Tests/`** - Unit tests (.NET 9.0)
- **`src/AiGeekSquad.AIContext.Benchmarks/`** - Performance benchmarks (.NET 9.0)
- **`src/AiGeekSquad.AIContext.MEAI/`** - Microsoft.Extensions.AI integration (.NET Standard 2.1)

### Solution File
- **Main solution**: `AiContext.slnx` - Use this for solution-wide operations
- Build entire solution: `dotnet build AiContext.slnx --configuration Release`

### Important Directories
- **`docs/`** - Documentation including detailed algorithm explanations
- **`examples/`** - Code examples (not complete projects, just .cs files)
- **`.github/workflows/`** - GitHub Actions for SonarQube analysis

## Development Guidelines

### Code Changes
- **Target Framework**: Main library uses .NET Standard 2.1 for broad compatibility
- **Test Projects**: Use .NET 9.0 and require .NET 9.0 SDK
- **Dependencies**: MathNet.Numerics, Microsoft.ML.Tokenizers core dependencies
- **Always run tests**: After any changes, run `dotnet test` to verify functionality

### Performance Considerations
- **Use Release builds**: For benchmarks and performance testing
- **Benchmark timing expectations**:
  - MMR algorithm: ~2ms for 1,000 vectors (384 dimensions)
  - Memory allocation: ~120KB per 1,000 vectors
  - Semantic chunking: Varies by document size and caching

### Common Issues
- **Missing .NET 9.0**: Install using the exact commands above
- **PATH issues**: Always `export PATH="/home/runner/.dotnet:$PATH"` after installation
- **Build failures**: Ensure you have .NET 9.0 SDK, not just runtime
- **Test timeouts**: Tests should complete in <5 seconds, benchmarks take 2-10+ minutes

## Library Core Functionality

### Maximum Marginal Relevance (MMR)
- **Purpose**: Diverse document selection balancing relevance and diversity
- **Usage**: `MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda, topK)`
- **Performance**: O(n²k) complexity, highly optimized for .NET 9.0

### Semantic Text Chunking
- **Purpose**: Intelligent text splitting based on semantic similarity
- **Components**: Token counting, embedding generation, similarity calculation
- **Streaming**: Uses `IAsyncEnumerable` for memory efficiency

### Ranking Engine
- **Purpose**: Multi-criteria ranking with customizable scoring functions
- **Features**: Multiple normalization strategies, combination strategies
- **Extensible**: Custom scoring functions and strategies supported

## Code Architecture & Patterns

### Interface-Driven Design (CRITICAL)

The AIContext library follows strict interface-first patterns. Always implement interfaces before concrete classes:

```csharp
// ✅ Correct: Interface-first with comprehensive documentation
/// <summary>
/// Provides functionality for custom similarity calculations.
/// </summary>
/// <param name="vector1">First vector for comparison.</param>
/// <param name="vector2">Second vector for comparison.</param>
/// <returns>Similarity score between 0.0 and 1.0.</returns>
public interface ICustomSimilarityCalculator
{
    Task<double> CalculateSimilarityAsync(Vector<double> vector1, Vector<double> vector2, CancellationToken cancellationToken = default);
}

// ✅ Correct: Streaming pattern for large datasets
public async IAsyncEnumerable<ProcessedChunk> ProcessLargeDocumentAsync(
    string document,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var chunk in chunker.ChunkAsync(document, cancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await ProcessChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
    }
}
```

### Async & Cancellation Patterns (CRITICAL)

**Every async operation MUST support cancellation tokens properly:**

```csharp
// ✅ Correct: Proper cancellation token patterns
public async IAsyncEnumerable<TextChunk> ChunkAsync(
    string text,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var segment in textSplitter.SplitAsync(text, cancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested(); // Check periodically

        var embedding = await embeddingGenerator.GenerateEmbeddingAsync(
            segment.Text, cancellationToken).ConfigureAwait(false); // Always use ConfigureAwait(false) in library code

        yield return new TextChunk(segment, embedding);
    }
}

// ❌ NEVER do this: Missing cancellation tokens
public async Task<Vector<double>> GenerateEmbeddingAsync(string text)
{
    return await httpClient.PostAsync(endpoint, content); // Cannot be cancelled!
}
```

**Cancellation Token Rules:**
1. **Always accept** `CancellationToken cancellationToken = default`
2. **Always pass through** to nested async calls
3. **Check periodically** with `ThrowIfCancellationRequested()`
4. **Use ConfigureAwait(false)** in all library code
5. **Test cancellation** in all unit tests

### TimeProvider Patterns (CRITICAL)

Use TimeProvider for all time operations to enable deterministic testing:

```csharp
// ✅ Correct: Accept TimeProvider with System default
public class ContextRenderer
{
    private readonly TimeProvider _timeProvider;

    public ContextRenderer(
        ITokenCounter tokenCounter,
        IEmbeddingGenerator embeddingGenerator,
        TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        var timestamp = _timeProvider.GetUtcNow(); // Use TimeProvider, not DateTime.UtcNow
        var item = new ContextItem(message.Content, embedding, tokenCount, timestamp);
    }
}
```

### Microsoft.Extensions.AI Integration

The MEAI project provides seamless integration with Microsoft's AI ecosystem:

```csharp
// ✅ Correct: Using MEAI integration
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, OpenAIEmbeddingGenerator>();
services.AddSingleton<SemanticTextChunker>();

// Integration with AIContext library
var chunker = serviceProvider.GetService<SemanticTextChunker>();
await foreach (var chunk in chunker.ChunkAsync(document, cancellationToken))
{
    // Process semantically chunked content
}
```

## Dependencies and Packaging

### Key Dependencies
- **MathNet.Numerics v5.0.0** - Vector operations and similarity calculations
- **Microsoft.ML.Tokenizers v1.0.2** - Accurate token counting (GPT-4 compatible)
- **Markdig v0.41.3** - Markdown processing

### NuGet Package
- **Package ID**: `AiGeekSquad.AIContext`
- **Target**: .NET Standard 2.1 for broad compatibility
- **Build for packaging**: `dotnet pack AiContext.slnx --configuration Release --no-build --output packages`

## Testing Patterns with xUnit v3 (CRITICAL)

The AIContext library uses xUnit v3 and requires comprehensive async and cancellation testing:

```csharp
// ✅ Correct: Async method testing with cancellation
[Fact]
public async Task ChunkAsync_WithValidText_ReturnsExpectedChunks()
{
    // Arrange
    var chunker = CreateSemanticChunker();
    var testDocument = CreateRealisticDocument();
    using var cts = new CancellationTokenSource();

    // Act
    var chunks = new List<TextChunk>();
    await foreach (var chunk in chunker.ChunkAsync(testDocument, cts.Token))
    {
        chunks.Add(chunk);
    }

    // Assert
    chunks.Should().NotBeEmpty();
    chunks.Should().AllSatisfy(chunk => chunk.Embedding.Should().NotBeNull());
}

// ✅ Correct: Testing cancellation behavior
[Fact]
public async Task ChunkAsync_WhenCancelled_ThrowsOperationCancelledException()
{
    // Arrange
    var chunker = CreateSemanticChunker();
    var largeDocument = CreateLargeDocument();
    using var cts = new CancellationTokenSource();

    // Act & Assert
    var enumerator = chunker.ChunkAsync(largeDocument, cts.Token).GetAsyncEnumerator();
    cts.Cancel(); // Cancel immediately

    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
        await enumerator.MoveNextAsync();
    });

    await enumerator.DisposeAsync(); // Always dispose async enumerators
}

// ✅ Correct: TimeProvider testing with FakeTimeProvider
[Fact]
public async Task Cache_WithExpiration_RemovesOldEntries()
{
    // Arrange
    var fakeTimeProvider = new FakeTimeProvider();
    var cache = new EmbeddingCache(TimeSpan.FromMinutes(5), fakeTimeProvider);

    // Add item to cache
    var embedding = CreateTestEmbedding();
    cache.Add("key1", embedding);

    // Advance time past expiration
    fakeTimeProvider.Advance(TimeSpan.FromMinutes(6));

    // Assert - item should be expired
    cache.TryGet("key1", out _).Should().BeFalse();
}
```

**Testing Requirements:**
- ✅ **Use realistic data** (384-dimension vectors, not trivial test cases)
- ✅ **Test cancellation behavior** for all async operations
- ✅ **Dispose async enumerators** with `await enumerator.DisposeAsync()`
- ✅ **Use FakeTimeProvider** for time-dependent tests
- ✅ **Set reasonable timeouts** (30s integration, 5s unit tests)
- ✅ **Test error conditions** with null/empty inputs
- ❌ **Never use .Result** in async tests (causes deadlocks)
- ❌ **Never skip cancellation testing** for async methods

## .NET CLI Usage (CRITICAL: MANDATORY)

**ALWAYS use `dotnet` CLI for ALL project operations. NEVER manually edit project files.**

```bash
# ✅ SOLUTION OPERATIONS
dotnet build AiContext.slnx --configuration Release
dotnet restore AiContext.slnx
dotnet test AiContext.slnx --configuration Release
dotnet pack AiContext.slnx --configuration Release --output packages

# ✅ PACKAGE MANAGEMENT
dotnet add src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package MathNet.Numerics --version 5.0.0
dotnet remove src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package OldPackage
dotnet list src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package

# ✅ PROJECT REFERENCES
dotnet add src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj reference src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj

# ✅ PROJECT CREATION (follow naming conventions)
dotnet new classlib -n AiGeekSquad.AIContext.NewFeature --framework netstandard2.1 -o src/AiGeekSquad.AIContext.NewFeature
dotnet new xunit -n AiGeekSquad.AIContext.NewFeature.Tests --framework net9.0 -o src/AiGeekSquad.AIContext.NewFeature.Tests
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj

# ❌ NEVER manually edit these files:
# ❌ DON'T: vim AiContext.slnx
# ❌ DON'T: vim src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj
```

**Project Structure Guidelines:**
- **Libraries**: `.NET Standard 2.1` for broad compatibility
- **Tests/Benchmarks**: `.NET 9.0` for latest features
- **Naming**: `AiGeekSquad.AIContext.[FeatureName]`
- **Verification**: Always run `dotnet build AiContext.slnx` after changes

## Timeout Requirements - CRITICAL

### Build Operations
- `dotnet restore`: 30 seconds timeout
- `dotnet build`: 60 seconds timeout
- `dotnet test`: 30 seconds timeout

### Benchmark Operations - NEVER CANCEL
- **MMR benchmarks**: 300+ seconds timeout. NEVER CANCEL.
- **Semantic benchmarks**: 600+ seconds timeout. NEVER CANCEL.
- **All benchmarks**: 1200+ seconds timeout. NEVER CANCEL.
- **Individual benchmark runs can take 2-10+ minutes. This is NORMAL.**

### Examples and Test Programs
- Simple test programs: 60 seconds timeout
- Library functionality tests: 30 seconds timeout

## Performance & Quality Standards (CRITICAL)

### Performance Expectations
- **MMR Algorithm**: ~2ms for 1,000 vectors (384 dimensions), O(n²k) complexity
- **Memory allocation**: ~120KB per 1,000 vectors processed
- **Semantic chunking**: Varies by document size and caching effectiveness
- **Vector normalization**: Always normalize to unit length for consistent cosine similarity
- **Lambda values**: Use 0.3-0.7 for most practical MMR applications

### Code Quality Requirements
- **XML Documentation**: All public APIs must have comprehensive `<summary>`, `<param>`, `<returns>` sections
- **Test Coverage**: Minimum 90% coverage for all new code
- **FluentAssertions**: Keep at version 7.2.0 (license constraint)
- **Real Implementation Testing**: Minimal mocking of core algorithms (test actual MMR/chunking logic)
- **Edge Case Handling**: Test with empty collections, null inputs, extreme lambda values (0.0, 1.0)

### Common Anti-Patterns to Avoid

```csharp
// ❌ DON'T: Bypass interfaces for "performance"
public class DirectEmbeddingChunker
{
    private readonly OpenAIClient openAIClient; // Tight coupling - breaks architecture
}

// ❌ DON'T: Blocking async operations
public List<Vector<double>> GenerateEmbeddings(List<string> texts)
{
    return texts.Select(text => embeddingGenerator.GenerateEmbeddingAsync(text).Result).ToList(); // Deadlock risk
}

// ❌ DON'T: Load entire streams into memory
public async Task<List<TextChunk>> ProcessEntireDocument(string massiveDocument)
{
    var allChunks = new List<TextChunk>();
    await foreach (var chunk in chunker.ChunkAsync(massiveDocument))
    {
        allChunks.Add(chunk); // Defeats streaming benefits
    }
    return allChunks;
}

// ❌ DON'T: Use DateTime.UtcNow directly
public async Task AddMessageAsync(ChatMessage message)
{
    var timestamp = DateTime.UtcNow; // Not testable with FakeTimeProvider
}
```

### Validation Checklist

Before completing any work, verify:

**Architecture Compliance:**
- [ ] Used interface-first design patterns
- [ ] Followed dependency injection patterns
- [ ] Used `IAsyncEnumerable` for streaming operations
- [ ] Maintained .NET Standard 2.1 compatibility for libraries

**Async/Cancellation:**
- [ ] All async methods accept cancellation tokens
- [ ] Used `ConfigureAwait(false)` in library code
- [ ] Tested cancellation behavior with `OperationCanceledException`
- [ ] Proper async enumerable disposal patterns

**Testing:**
- [ ] Used realistic test data (384-dimension vectors)
- [ ] Comprehensive cancellation token testing
- [ ] Used FakeTimeProvider for time-dependent tests
- [ ] Achieved >90% test coverage

**Performance:**
- [ ] Considered O(n²k) complexity implications for MMR
- [ ] Used streaming patterns for large datasets
- [ ] Benchmarked performance-critical changes
- [ ] Memory usage patterns validated

**Project Management:**
- [ ] Used `dotnet` CLI for all project operations
- [ ] Followed naming conventions (AiGeekSquad.AIContext.*)
- [ ] Verified solution builds with `dotnet build AiContext.slnx`
- [ ] Updated documentation and examples

## Common Commands Reference

```bash
# Setup (run once)
cd /tmp && wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.303
export PATH="/home/runner/.dotnet:$PATH"

# Basic workflow
dotnet restore                                    # ~9s
dotnet build --configuration Release             # ~3.7s  
dotnet test                                       # ~2.8s

# Benchmarks (NEVER CANCEL - set 600+ second timeouts)
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release mmr      # ~2min
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release semantic # ~3min

# Validation
dotnet build && dotnet test && echo "All good!"
```

## Additional Resources

For comprehensive guidance beyond GitHub Copilot usage:

- **[CLAUDE.md](../CLAUDE.md)**: Complete project overview, architecture details, and development patterns
- **[AGENTS.md](../AGENTS.md)**: Advanced AI agent workflows, collaboration patterns, and detailed technical guidance
- **[docs/](../docs/)**: Technical documentation including algorithm explanations and performance tuning guides
- **[examples/](../examples/)**: Working code examples for all major features

**Key Integration Points:**
- This Copilot guidance focuses on practical development patterns and immediate coding standards
- CLAUDE.md provides architectural context and comprehensive command references
- AGENTS.md contains advanced workflow patterns for complex tasks and agent collaboration
- All three documents work together to ensure consistent, high-quality development practices

**When to Consult Other Guides:**
- **Complex Architecture Questions**: Refer to CLAUDE.md for detailed component explanations
- **Advanced Testing Patterns**: Check AGENTS.md for comprehensive async/cancellation testing guidance
- **Performance Optimization**: Review CLAUDE.md performance sections and docs/ for detailed tuning guides
- **Multi-step Workflows**: Use AGENTS.md patterns for complex development scenarios