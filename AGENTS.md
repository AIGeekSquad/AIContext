# AGENTS.md

This file provides specialized guidance for AI agents (Claude Code, GitHub Copilot, etc.) when working with the AIContext library codebase.

## 💎 CRITICAL: Respect User Time - Test Before Presenting
 
**The user's time is their most valuable resource.** When you present work as "ready" or "done", you must have:
 
1. **Tested it yourself thoroughly** - Don't make the user your QA
2. **Fixed obvious issues** - Syntax errors, import problems, broken logic
3. **Verified it actually works** - Run tests, check structure, validate logic
4. **Only then present it** - "This is ready for your review" means YOU'VE already validated it
 
**User's role:** Strategic decisions, design approval, business context, stakeholder judgment
**Your role:** Implementation, testing, debugging, fixing issues before engaging user
 
**Anti-pattern**: "I've implemented X, can you test it and let me know if it works?"
**Correct pattern**: "I've implemented and tested X. Tests pass, structure verified, logic validated. Ready for your review. Here is how you can verify."
 
**Remember**: Every time you ask the user to debug something you could have caught, you're wasting their time on non-stakeholder work. Be thorough BEFORE engaging them.

## Agent Workflow Patterns

### Understanding the Codebase Architecture

When analyzing this library, focus on these key architectural patterns:

1. **Interface Segregation**: The library uses small, focused interfaces (`IEmbeddingGenerator`, `ITextSplitter`, etc.)
2. **Streaming Design**: Heavy use of `IAsyncEnumerable` for memory-efficient processing
3. **Mathematical Foundations**: All similarity calculations use MathNet.Numerics for consistency
4. **Performance-First**: Algorithms are optimized for production use with comprehensive benchmarks

### Code Analysis Workflow

When exploring or modifying code, follow this systematic approach:

1. **Start with interfaces** in `/Chunking/` and `/Ranking/` folders
2. **Examine the main implementations**: `SemanticTextChunker.cs`, `MaximumMarginalRelevance.cs`
3. **Review test files** for usage patterns and edge cases
4. **Check benchmarks** for performance expectations

### Key Files for Agent Understanding

```
Priority 1 - Core Architecture:
├── src/AiGeekSquad.AIContext/Chunking/IEmbeddingGenerator.cs
├── src/AiGeekSquad.AIContext/Chunking/SemanticTextChunker.cs
├── src/AiGeekSquad.AIContext/Ranking/MaximumMarginalRelevance.cs
└── src/AiGeekSquad.AIContext/Ranking/RankingEngine.cs

Priority 2 - Implementation Details:
├── src/AiGeekSquad.AIContext/Chunking/SentenceTextSplitter.cs
├── src/AiGeekSquad.AIContext/Chunking/MLTokenCounter.cs
└── src/AiGeekSquad.AIContext/Ranking/Strategies/

Priority 3 - Testing and Examples:
├── src/AiGeekSquad.AIContext.Tests/Chunking/SemanticChunkingTests.cs
├── src/AiGeekSquad.AIContext.Tests/Ranking/MaximumMarginalRelevanceTests.cs
└── examples/BasicChunking.cs
```

## Work Workflow Guidelines

Follow these essential workflow practices when working on the AIContext library:

### 1. Version Control Management
- **Create baseline commits** before starting any relevant work to capture the current state and enable easy restoration
- **Track progress with commits** as work progresses, ensuring code compiles at minimum before committing
- **Use descriptive commit messages** that reflect the current state and are useful for analyzing progress and understanding changes

### 2. Research and Context Gathering
- **Leverage available tools** - When tools like Tavily, DeepWiki, Context7, and Perplexity are available, always ensure you have the right context and most up-to-date information
- **Verify current library versions** used in the project before making recommendations or implementing changes
- **Stay informed** about the libraries and frameworks being used in the specific project context

### 3. Dependency Management
- **FluentAssertions package**: Do not upgrade FluentAssertions package beyond version 7.2.0 due to license changes
- **Version compatibility**: Always verify compatibility with existing project dependencies before suggesting upgrades

### 4. Development Practices
- **Test-Driven Development (TDD)**: Always create tests upfront and follow proper TDD practices
- **Incremental changes**: Make proper and incremental changes following a minimalistic and simplicity-first approach
- **Compile verification**: Ensure code compiles successfully before proceeding with further changes

### 5. Workspace Organization
- **Use `ai_working` folder** for any work that requires tracking state on disk, including:
  - Plan documents
  - Temporary work files
  - Progress tracking documents
  - Research notes and findings

### 6. Project Maintenance
- **Clean up thoroughly** at the end of tasks to keep the repository clean and focused
- **Remove temporary files** and working documents that are no longer needed
- **Maintain alignment** between code, documentation, and samples

### 7. Documentation Standards
- **Keep documentation current** - Ensure documentation is updated when tasks are completed
- **Maintain consistency** between code comments, external documentation, and example code
- **Verify examples work** with the current codebase implementation

## Code Generation Guidelines

### When Creating New Features

1. **Always implement interfaces first**: Define contracts before implementations
2. **Include comprehensive XML documentation**: This library has high documentation standards
3. **Add corresponding tests**: Minimum 90% coverage requirement
4. **Consider performance implications**: Add benchmarks for compute-intensive operations
5. **Follow async patterns**: Use `IAsyncEnumerable` for streaming operations

### Code Style Patterns

```csharp
// ✅ Correct: Interface-first design with comprehensive documentation
/// <summary>
/// Provides functionality for custom similarity calculations.
/// </summary>
/// <param name="vector1">First vector for comparison.</param>
/// <param name="vector2">Second vector for comparison.</param>
/// <returns>Similarity score between 0.0 and 1.0.</returns>
public interface ICustomSimilarityCalculator
{
    Task<double> CalculateSimilarityAsync(Vector<double> vector1, Vector<double> vector2);
}

// ✅ Correct: Streaming pattern for large datasets
public async IAsyncEnumerable<ProcessedChunk> ProcessLargeDocumentAsync(
    string document,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var chunk in chunker.ChunkAsync(document, cancellationToken))
    {
        yield return await ProcessChunkAsync(chunk, cancellationToken);
    }
}

// ❌ Avoid: Blocking synchronous operations for I/O
public List<Vector<double>> GenerateEmbeddings(List<string> texts)
{
    return texts.Select(text => embeddingGenerator.GenerateEmbeddingAsync(text).Result).ToList();
}
```

### Testing Pattern Recognition

When creating tests, follow these established patterns:

```csharp
// ✅ Pattern: Real implementations with meaningful data
[Fact]
public void ComputeMMR_WithBalancedLambda_ReturnsRelevantAndDiverseResults()
{
    // Arrange: Use realistic vector data
    var vectors = CreateTestVectors();
    var query = CreateQueryVector();

    // Act: Test actual algorithm
    var results = MaximumMarginalRelevance.ComputeMMR(vectors, query, lambda: 0.5, topK: 3);

    // Assert: Verify both correctness and performance characteristics
    Assert.Equal(3, results.Count);
    AssertRelevanceAndDiversity(results, query, vectors);
}

// ❌ Avoid: Over-mocking core algorithms
[Fact]
public void TestWithMocks()
{
    var mockMMR = new Mock<IMaximumMarginalRelevance>();
    // This doesn't test the actual algorithm implementation
}
```

## Agent Collaboration Patterns

### When Multiple Agents Work on This Codebase

1. **Benchmark Coordination**: Only one agent should run benchmarks at a time (they take 2-10+ minutes)
2. **Test Isolation**: Each agent should run full test suite before committing changes
3. **Documentation Updates**: Update both code comments and external docs simultaneously
4. **Version Compatibility**: Always verify .NET Standard 2.1 compatibility for main library

### Shared Understanding Checkpoints

Before making significant changes, verify understanding of:

- **MMR Algorithm**: Can you explain the lambda parameter's impact on relevance vs diversity?
- **Chunking Pipeline**: Do you understand the `SemanticTextChunker` workflow with embeddings and similarity?
- **Interface Contracts**: Are you clear on the async enumerable patterns for streaming?
- **Performance Expectations**: Do you know the O(n²k) complexity of MMR and memory usage patterns?

## Common Agent Pitfalls

### Performance-Related Issues

```csharp
// ❌ Pitfall: Not considering memory usage for large datasets
public async Task<List<TextChunk>> ProcessEntireDocument(string massiveDocument)
{
    var allChunks = new List<TextChunk>();
    await foreach (var chunk in chunker.ChunkAsync(massiveDocument))
    {
        allChunks.Add(chunk); // This defeats streaming benefits
    }
    return allChunks;
}

// ✅ Solution: Preserve streaming benefits
public async IAsyncEnumerable<TextChunk> ProcessDocumentStreamAsync(string document)
{
    await foreach (var chunk in chunker.ChunkAsync(document))
    {
        yield return chunk; // Memory-efficient streaming
    }
}
```

### Testing Mistakes

```csharp
// ❌ Pitfall: Testing with trivial data
[Fact]
public void TestMMRWithSimpleVectors()
{
    var vectors = new List<Vector<double>>
    {
        Vector<double>.Build.Dense(new double[] { 1, 0 }),
        Vector<double>.Build.Dense(new double[] { 0, 1 })
    };
    // This doesn't test realistic scenarios
}

// ✅ Solution: Use realistic high-dimensional data
[Fact]
public void TestMMRWithRealisticEmbeddings()
{
    var vectors = GenerateRealistic384DimensionVectors(count: 100);
    var query = GenerateQueryVector(384);
    // Tests real-world scenarios
}
```

### Architecture Violations

```csharp
// ❌ Pitfall: Bypassing interfaces for "performance"
public class DirectEmbeddingChunker
{
    private readonly OpenAIClient openAIClient; // Tight coupling

    public async Task<List<Chunk>> ChunkDirectly(string text)
    {
        // Bypasses IEmbeddingGenerator interface
    }
}

// ✅ Solution: Respect interface boundaries
public class SemanticChunkerService
{
    private readonly IEmbeddingGenerator _embeddingGenerator;

    public SemanticChunkerService(IEmbeddingGenerator embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator; // Proper DI
    }
}
```

## Agent-Specific Best Practices

### Code Analysis Tasks

1. **Performance Analysis**: Always check if new code affects the O(n²k) complexity of MMR
2. **Memory Profiling**: Consider memory usage patterns, especially for streaming operations
3. **Async Correctness**: Verify proper `ConfigureAwait(false)` usage in library code
4. **Vector Operations**: Ensure all similarity calculations use consistent MathNet patterns

### Documentation Tasks

1. **XML Comments**: Include `<param>`, `<returns>`, `<example>` sections for public APIs
2. **Performance Notes**: Document complexity and memory usage for algorithms
3. **Usage Examples**: Provide realistic examples with actual vector dimensions
4. **Edge Cases**: Document behavior with empty collections, null inputs, extreme lambda values

### Testing Tasks

1. **Edge Case Coverage**: Test with empty vectors, single vectors, lambda extremes (0.0, 1.0)
2. **Performance Regression**: Add benchmark tests for new algorithms
3. **Integration Testing**: Test complete workflows from text input to final results
4. **Cancellation Support**: Verify all async operations respect cancellation tokens

## Agent Validation Checklist

Before completing any task on this codebase:

- [ ] Does the code follow the interface-first pattern?
- [ ] Are all public methods documented with XML comments?
- [ ] Do async methods use `IAsyncEnumerable` where appropriate?
- [ ] Are tests added with >90% coverage?
- [ ] Does `dotnet test` pass with all 146+ tests?
- [ ] For performance-critical code, are benchmarks included?
- [ ] Does the code maintain .NET Standard 2.1 compatibility?
- [ ] Are proper cancellation token patterns used?
- [ ] Does the implementation respect the O(n²k) performance characteristics?

## Integration with Existing Tools

### Working with Benchmarks

```bash
# Agent workflow for performance validation
dotnet build --configuration Release
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release all

# Never cancel benchmarks - they provide critical performance data
# Set timeouts to 600+ seconds for benchmark operations
```

### Working with CI/CD

- **SonarQube Integration**: Ensure code coverage reports are generated correctly
- **AppVeyor Builds**: Test Windows compatibility with `dotnet build AiContext.slnx --configuration Release`
- **NuGet Packaging**: Use `dotnet pack` for package generation

This guidance should help AI agents work effectively with the AIContext library while maintaining code quality, performance standards, and architectural consistency.