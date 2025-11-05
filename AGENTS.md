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
├── examples/BasicChunking.cs
├── examples/MMRExample.cs
├── examples/ProductSearchDemo.cs
├── examples/SupportTicketRouter.cs
└── examples/EnterpriseRAGService.cs
```

## Work Workflow Guidelines

Follow these essential workflow practices when working on the AIContext library:

### 1. `ai_working` Folder Management (FIRST PRIORITY)
- **ALWAYS start by checking `ai_working/README.md`** - this is MANDATORY, not optional
- **Read existing content completely** before creating any new files or analysis
- **Update README.md immediately** when you start your session with current task info
- **Reference existing work** in any new content you create - build incrementally
- **Document your progress** by updating README.md throughout your session
- **Leave clear handoff** by updating README.md with status and next steps when finishing

### 2. Version Control Management
- **Create baseline commits** before starting any relevant work to capture the current state and enable easy restoration
- **Track progress with commits** as work progresses, ensuring code compiles at minimum before committing
- **Use descriptive commit messages** that reflect the current state and are useful for analyzing progress and understanding changes

### 3. Research and Context Gathering
- **Leverage available tools** - When tools like Tavily, DeepWiki, Context7, and Perplexity are available, always ensure you have the right context and most up-to-date information
- **Verify current library versions** used in the project before making recommendations or implementing changes
- **Stay informed** about the libraries and frameworks being used in the specific project context
- **Check `ai_working/research/` folder** for existing research before starting new context gathering

### 4. Dependency Management
- **FluentAssertions constraint**: Do not upgrade FluentAssertions beyond version 7.2.0 due to license changes
- **Version compatibility**: Always verify compatibility with existing project dependencies before suggesting upgrades
- **Microsoft.Extensions.AI integration**: The MEAI project provides seamless integration with Microsoft's AI abstractions

### 5. Development Practices
- **High Quality standards**: Always use SonarQube for quality monitoring and creating high quality code when available
- **Test-Driven Development (TDD)**: Always create tests upfront and follow proper TDD practices
- **Incremental changes**: Make proper and incremental changes following a minimalistic and simplicity-first approach
- **Compile verification**: Ensure code compiles successfully before proceeding with further changes
- **Async patterns**: Follow proper cancellation token patterns and xUnit v3 testing practices (detailed below)

### 6. .NET CLI Usage (CRITICAL: Never Edit Project Files Manually)

**ALWAYS use the `dotnet` CLI for all project operations. NEVER manually edit `.slnx`, `.csproj`, `.sln`, or other project files unless absolutely necessary.**

#### 🔴 MANDATORY: Use dotnet CLI for All Project Operations

**The AIContext project uses modern .NET tooling. Always use the CLI to ensure consistency, proper validation, and MSBuild integration.**

#### ✅ Common .NET CLI Operations

```bash
# 🏗️ SOLUTION OPERATIONS
# Build entire solution
dotnet build AiContext.slnx --configuration Release

# Restore packages for entire solution
dotnet restore AiContext.slnx

# Test entire solution
dotnet test AiContext.slnx --configuration Release

# Pack all packable projects
dotnet pack AiContext.slnx --configuration Release --output packages

# Add existing project to solution
dotnet sln AiContext.slnx add src/NewProject/NewProject.csproj

# Remove project from solution
dotnet sln AiContext.slnx remove src/OldProject/OldProject.csproj

# List projects in solution
dotnet sln AiContext.slnx list

# 📦 PACKAGE MANAGEMENT
# Add NuGet package to specific project
dotnet add src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package Microsoft.Extensions.AI

# Add package with specific version
dotnet add src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package MathNet.Numerics --version 5.0.0

# Remove package from project
dotnet remove src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package OldPackage

# Update packages (be careful with FluentAssertions - keep at 7.2.0!)
dotnet add src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj package xunit --version latest

# List packages in project
dotnet list src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package

# 🔗 PROJECT REFERENCES
# Add project reference
dotnet add src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj reference src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj

# Remove project reference
dotnet remove src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj reference src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj

# List project references
dotnet list src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj reference

# 🆕 PROJECT CREATION
# Create new class library (.NET Standard 2.1 to match main library)
dotnet new classlib -n AiGeekSquad.AIContext.NewFeature --framework netstandard2.1 -o src/AiGeekSquad.AIContext.NewFeature

# Create new test project (use .NET 9.0 like existing tests)
dotnet new xunit -n AiGeekSquad.AIContext.NewFeature.Tests --framework net9.0 -o src/AiGeekSquad.AIContext.NewFeature.Tests

# Create new benchmark project (use .NET 9.0)
dotnet new console -n AiGeekSquad.AIContext.NewBenchmarks --framework net9.0 -o src/AiGeekSquad.AIContext.NewBenchmarks

# Add new projects to solution
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj

# 🔧 PROJECT CONFIGURATION
# Set project properties via CLI (preferred over manual editing)
dotnet add src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj package Microsoft.SourceLink.GitHub --version 1.1.1
```

#### ✅ AIContext-Specific .NET CLI Patterns

```bash
# 🎯 ADDING A NEW FEATURE LIBRARY
# Step 1: Create the library project
dotnet new classlib -n AiGeekSquad.AIContext.NewFeature --framework netstandard2.1 -o src/AiGeekSquad.AIContext.NewFeature

# Step 2: Add it to the solution
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj

# Step 3: Add required dependencies (following existing patterns)
dotnet add src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj package MathNet.Numerics --version 5.0.0

# Step 4: Reference main library if needed
dotnet add src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj reference src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj

# 🧪 ADDING TESTS FOR NEW FEATURE
# Step 1: Create test project
dotnet new xunit -n AiGeekSquad.AIContext.NewFeature.Tests --framework net9.0 -o src/AiGeekSquad.AIContext.NewFeature.Tests

# Step 2: Add to solution
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj

# Step 3: Add test dependencies (matching existing test projects)
dotnet add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj package FluentAssertions --version 7.2.0
dotnet add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj package Microsoft.NET.Test.Sdk
dotnet add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj package xunit.runner.visualstudio

# Step 4: Add project references
dotnet add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj reference src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj
dotnet add src/AiGeekSquad.AIContext.NewFeature.Tests/AiGeekSquad.AIContext.NewFeature.Tests.csproj reference src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj

# 📊 ADDING BENCHMARKS
# Step 1: Create benchmark project
dotnet new console -n AiGeekSquad.AIContext.NewFeature.Benchmarks --framework net9.0 -o src/AiGeekSquad.AIContext.NewFeature.Benchmarks

# Step 2: Add to solution
dotnet sln AiContext.slnx add src/AiGeekSquad.AIContext.NewFeature.Benchmarks/AiGeekSquad.AIContext.NewFeature.Benchmarks.csproj

# Step 3: Add BenchmarkDotNet (matching existing benchmark projects)
dotnet add src/AiGeekSquad.AIContext.NewFeature.Benchmarks/AiGeekSquad.AIContext.NewFeature.Benchmarks.csproj package BenchmarkDotNet

# Step 4: Add project references
dotnet add src/AiGeekSquad.AIContext.NewFeature.Benchmarks/AiGeekSquad.AIContext.NewFeature.Benchmarks.csproj reference src/AiGeekSquad.AIContext.NewFeature/AiGeekSquad.AIContext.NewFeature.csproj
```

#### ❌ NEVER Do These Manual Operations

```bash
# ❌ DON'T: Manually edit .slnx files
vim AiContext.slnx  # DON'T DO THIS

# ❌ DON'T: Manually edit .csproj files for package references
vim src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj  # DON'T DO THIS

# ❌ DON'T: Copy/paste project references between files
# ❌ DON'T: Manually edit version numbers in project files
# ❌ DON'T: Hand-edit MSBuild properties without using CLI
```

#### ✅ When Manual Editing is Acceptable

```xml
<!-- ✅ ACCEPTABLE: Complex MSBuild properties not available via CLI -->
<PropertyGroup>
  <PackageDescription>Detailed description of the AI Context library...</PackageDescription>
  <PackageTags>AI;ML;Embeddings;Chunking;MMR;Similarity</PackageTags>
  <RepositoryUrl>https://github.com/AiGeekSquad/AIContext</RepositoryUrl>
  <AssemblyTitle>AI Context Library</AssemblyTitle>
</PropertyGroup>

<!-- ✅ ACCEPTABLE: Conditional compilation symbols -->
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <DefineConstants>TRACE;DEBUG</DefineConstants>
</PropertyGroup>

<!-- ✅ ACCEPTABLE: Complex item groups not supported by CLI -->
<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  <None Include="..\..\LICENSE" Pack="true" PackagePath="\" />
</ItemGroup>
```

#### 🔍 Verification Commands

```bash
# ✅ Verify solution integrity after changes
dotnet build AiContext.slnx --no-restore --verbosity minimal

# ✅ Check that all projects can be built independently
dotnet build src/AiGeekSquad.AIContext/AiGeekSquad.AIContext.csproj
dotnet build src/AiGeekSquad.AIContext.Tests/AiGeekSquad.AIContext.Tests.csproj

# ✅ Verify package versions are consistent
dotnet list package --include-transitive --format json > ai_working/analysis/package_analysis.json

# ✅ Check for package vulnerabilities
dotnet list package --vulnerable --include-transitive

# ✅ Verify test discovery
dotnet test --list-tests --logger trx --verbosity quiet
```

#### 🏗️ Project Structure Best Practices

**Follow the existing AIContext project structure:**

```
src/
├── AiGeekSquad.AIContext/              # Main library (.NET Standard 2.1)
├── AiGeekSquad.AIContext.Tests/        # Unit tests (.NET 9.0)
├── AiGeekSquad.AIContext.Benchmarks/   # Benchmarks (.NET 9.0)
├── AiGeekSquad.AIContext.MEAI/         # Microsoft.Extensions.AI integration
└── [YourNewProject]/                   # Follow naming convention
```

**Naming Conventions:**
- Libraries: `AiGeekSquad.AIContext.[FeatureName]`
- Tests: `AiGeekSquad.AIContext.[FeatureName].Tests`
- Benchmarks: `AiGeekSquad.AIContext.[FeatureName].Benchmarks`

**Target Frameworks:**
- Libraries: `.NET Standard 2.1` (for broad compatibility)
- Tests/Benchmarks: `.NET 9.0` (for latest performance and test features)

### 7. Workspace Organization (CRITICAL: MANDATORY README.md MAINTENANCE)

**The `ai_working` folder is a living workspace that MUST be actively maintained and utilized across agent sessions.**

#### 🔴 MANDATORY: Always Start by Checking `ai_working/README.md`

**Before starting any task, ALWAYS:**
1. **Check if `ai_working/README.md` exists** - read it completely to understand current state
2. **Review existing content** - use what's already been researched/planned instead of duplicating work
3. **Update README.md** when adding, modifying, or removing any content
4. **Reference existing work** - build upon previous analysis rather than starting from scratch

#### ✅ Proper `ai_working` Folder Workflow

```bash
# STEP 1: Always check existing state first
ls ai_working/ 2>/dev/null || echo "No ai_working folder yet"
cat ai_working/README.md 2>/dev/null || echo "No README.md - will create one"

# STEP 2: Review existing content before creating new files
find ai_working/ -type f -name "*.md" -o -name "*.txt" -o -name "*.cs" 2>/dev/null

# STEP 3: Work with existing content or create new as needed
# STEP 4: ALWAYS update README.md when done
```

#### 🗂️ Mandatory `ai_working/README.md` Structure

**Every `ai_working` folder MUST contain a README.md with this structure:**

```markdown
# AI Working Directory

**Last Updated**: [Date/Time] by [Agent/Session ID]
**Current Task**: [Brief description of what you're working on]

## 📋 Contents Overview

### 🎯 Active Work
- `plans/current_task.md` - [Brief description and status]
- `drafts/implementation.cs` - [What this contains and current state]

### 📚 Research & Context
- `research/performance_analysis.md` - [Key findings and relevance]
- `research/library_versions.md` - [Current dependency information]

### 🧪 Analysis Results
- `analysis/benchmark_results.md` - [Performance data and conclusions]
- `analysis/code_review_findings.md` - [Issues found and resolutions]

### 🗄️ Archive (Completed)
- `archive/previous_implementation.cs` - [Completed work, kept for reference]

## 🎯 Current Status
[Detailed description of current work state, what's been accomplished, what's next]

## 🔗 Dependencies & Context
[Links to related files, external resources, prerequisites for understanding the work]

## ⚠️ Important Notes
[Critical information that future agents need to know]

## 📝 Session Log
- **[Date]**: [Agent] - [Brief description of work done]
- **[Date]**: [Agent] - [Brief description of work done]
```

#### 📂 Enhanced Folder Structure with State Management

```
ai_working/
├── README.md                    # MANDATORY: Current state documentation
├── plans/
│   ├── current_task.md         # Active planning document
│   ├── architectural_decisions.md
│   └── implementation_roadmap.md
├── research/
│   ├── context_gathering.md    # External research findings
│   ├── library_analysis.md     # Current codebase analysis
│   └── performance_requirements.md
├── drafts/
│   ├── code_samples/          # Work-in-progress implementations
│   ├── test_examples/         # Draft test cases
│   └── documentation_drafts/  # Documentation being developed
├── analysis/
│   ├── benchmark_results.md   # Performance testing results
│   ├── code_review.md        # Quality analysis findings
│   └── dependency_analysis.md # Library compatibility checks
├── temp/                      # Short-lived experimental files
└── archive/                   # Completed work kept for reference
```

#### 🔄 State Management Workflow

**When Starting Work:**
1. **Read `ai_working/README.md`** completely - understand current state
2. **Check relevant subdirectories** for existing work you can build upon
3. **Update README.md** with your session information and current task
4. **Reference existing files** instead of recreating similar content

**During Work:**
1. **Update README.md** whenever you add/modify/remove files
2. **Cross-reference existing work** - link to related files in your new content
3. **Build incrementally** - extend existing analysis rather than starting over
4. **Document decisions** - record why you chose specific approaches

**When Finishing Work:**
1. **Update README.md** with final status and next steps
2. **Organize temp files** - move important work to appropriate folders, leave cleanup decisions to users
3. **Archive completed items** - move clearly finished work to `archive/` with clear naming
4. **Leave clear handoff** - document state for future agents
5. **Document cleanup recommendations** - note in README.md what could be cleaned up (let users decide)

#### ❌ Common `ai_working` Mistakes to Avoid

```bash
# ❌ MISTAKE: Starting work without checking existing state
echo "Starting fresh analysis..." > ai_working/analysis/new_analysis.md
# This ignores existing analysis/previous_analysis.md that might be relevant

# ❌ MISTAKE: Not updating README.md when adding files
cp important_findings.md ai_working/research/
# README.md still shows old state, future agents won't know this exists

# ❌ MISTAKE: Creating duplicate work
# Agent 1 creates: ai_working/plans/performance_plan.md
# Agent 2 creates: ai_working/plans/optimization_plan.md (similar content)
# Should have read README.md and built upon existing work

# ❌ MISTAKE: Not referencing existing work in new files
echo "Based on my analysis..." > ai_working/analysis/new_findings.md
# Should reference: "Building on analysis/previous_findings.md, I found..."
```

#### ✅ Correct `ai_working` Usage Patterns

```bash
# ✅ CORRECT: Always check and update README.md
cat ai_working/README.md
# [Read and understand current state]
echo "## New Session $(date)" >> ai_working/README.md
echo "Working on: Performance optimization" >> ai_working/README.md

# ✅ CORRECT: Reference existing work
echo "Extending analysis from research/performance_baseline.md..." > ai_working/analysis/optimization_results.md

# ✅ CORRECT: Update README when adding content
echo "- analysis/optimization_results.md - Performance improvement findings" >> ai_working/README.md

# ✅ CORRECT: Clean handoff for next agent
echo "## Next Steps" >> ai_working/README.md
echo "1. Implement findings from analysis/optimization_results.md" >> ai_working/README.md
echo "2. Run benchmarks to validate improvements" >> ai_working/README.md
```

#### 🔒 Git Integration Rules

- **Always add `ai_working/` to `.gitignore`** to prevent accidental commits
- **Exception**: If work produces valuable artifacts, copy them to appropriate project folders
- **Documentation**: Update project documentation based on `ai_working` findings
- **Clean transitions**: Move completed work from `ai_working` to project structure when appropriate
- **User-controlled cleanup**: Since `ai_working/` is gitignored, leave full cleanup decisions to users - just document recommendations

### 8. Project Maintenance
- **Clean up project files thoroughly** at the end of tasks to keep the repository clean and focused
- **Remove temporary project files** and working documents from the main project structure (not `ai_working/`)
- **Maintain alignment** between code, documentation, and samples
- **Note**: `ai_working/` cleanup is user-controlled since it's gitignored

### 9. Documentation Standards
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

### Cancellation Token Patterns (CRITICAL)

**The AIContext library heavily uses async operations and streaming. Proper cancellation token usage is essential for performance, reliability, and testability.**

#### ✅ Correct Cancellation Token Patterns

```csharp
// ✅ Correct: Always accept and pass through cancellation tokens
public async IAsyncEnumerable<TextChunk> ChunkAsync(
    string text,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var segment in textSplitter.SplitAsync(text, cancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested(); // Check periodically

        var embedding = await embeddingGenerator.GenerateEmbeddingAsync(
            segment.Text, cancellationToken);

        yield return new TextChunk(segment, embedding);
    }
}

// ✅ Correct: Respect cancellation in loops and iterations
public async Task<MMRResult[]> ComputeMMRAsync(
    IReadOnlyList<Vector<double>> vectors,
    Vector<double> query,
    double lambda,
    int topK,
    CancellationToken cancellationToken = default)
{
    var selected = new List<MMRResult>();

    for (int i = 0; i < topK && i < vectors.Count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested(); // Check each iteration

        // Compute MMR scores (potentially expensive operation)
        var bestIndex = await ComputeBestScoreAsync(vectors, selected, query, lambda, cancellationToken);
        selected.Add(new MMRResult(bestIndex, vectors[bestIndex]));
    }

    return selected.ToArray();
}

// ✅ Correct: Propagate cancellation through the entire chain
public async Task<SemanticChunk[]> ProcessDocumentAsync(
    string document,
    CancellationToken cancellationToken = default)
{
    var chunks = new List<SemanticChunk>();

    await foreach (var chunk in semanticChunker.ChunkAsync(document, cancellationToken))
    {
        cancellationToken.ThrowIfCancellationRequested();
        chunks.Add(chunk);
    }

    return chunks.ToArray();
}
```

#### ❌ Common Cancellation Token Mistakes

```csharp
// ❌ Mistake: Not accepting cancellation tokens
public async Task<Vector<double>> GenerateEmbeddingAsync(string text)
{
    // Cannot be cancelled - blocks indefinitely
    return await httpClient.PostAsync(endpoint, content);
}

// ❌ Mistake: Not passing cancellation tokens through
public async IAsyncEnumerable<TextChunk> ChunkAsync(string text)
{
    await foreach (var segment in textSplitter.SplitAsync(text)) // Missing cancellationToken
    {
        var embedding = await embeddingGenerator.GenerateEmbeddingAsync(segment.Text); // Missing cancellationToken
        yield return new TextChunk(segment, embedding);
    }
}

// ❌ Mistake: Not checking cancellation in long-running operations
public async Task<MMRResult[]> ComputeMMRAsync(
    IReadOnlyList<Vector<double>> vectors,
    Vector<double> query,
    double lambda,
    int topK,
    CancellationToken cancellationToken = default)
{
    for (int i = 0; i < topK; i++)
    {
        // Long running loop without cancellation checks
        // This can run indefinitely even if cancelled
        var bestScore = ComputeExpensiveScore(vectors, query);
    }
}

// ❌ Mistake: Using .Result or .Wait() with cancellation tokens
public Vector<double> GenerateEmbedding(string text, CancellationToken cancellationToken = default)
{
    return GenerateEmbeddingAsync(text, cancellationToken).Result; // Blocks and ignores cancellation
}
```

#### Cancellation Token Best Practices

1. **Always accept cancellation tokens**: Every async method should have a `CancellationToken` parameter
2. **Use [EnumeratorCancellation]**: For `IAsyncEnumerable` methods, use the attribute for proper binding
3. **Pass tokens through**: Always propagate cancellation tokens to nested async calls
4. **Check periodically**: Call `ThrowIfCancellationRequested()` in loops and long operations
5. **Default parameter**: Use `= default` to make cancellation tokens optional
6. **Library code patterns**: Use `ConfigureAwait(false)` for library code to avoid deadlocks
7. **Testing**: Always test cancellation behavior in unit tests

### xUnit v3 Testing Patterns (ESSENTIAL)

**The AIContext library requires comprehensive async and cancellation token testing. Use xUnit v3 patterns for reliable, fast tests.**

#### ✅ Correct xUnit v3 Async Testing Patterns

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

// ✅ Pattern: Async method testing with proper cancellation token handling
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
    Assert.NotEmpty(chunks);
    Assert.All(chunks, chunk => Assert.NotNull(chunk.Embedding));
    Assert.All(chunks, chunk => Assert.True(chunk.TokenCount > 0));
}

// ✅ Pattern: Testing cancellation token behavior
[Fact]
public async Task ChunkAsync_WhenCancelled_ThrowsOperationCancelledException()
{
    // Arrange
    var chunker = CreateSemanticChunker();
    var largeDocument = CreateLargeDocument(); // Simulate long-running operation
    using var cts = new CancellationTokenSource();

    // Act & Assert
    var enumerator = chunker.ChunkAsync(largeDocument, cts.Token).GetAsyncEnumerator();

    // Cancel immediately to test cancellation handling
    cts.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    {
        await enumerator.MoveNextAsync();
    });

    await enumerator.DisposeAsync();
}

// ✅ Pattern: Testing async enumerable with timeout
[Fact]
public async Task ChunkAsync_WithTimeout_CompletesWithinExpectedTime()
{
    // Arrange
    var chunker = CreateSemanticChunker();
    var document = CreateMediumSizeDocument();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Reasonable timeout

    // Act
    var chunks = new List<TextChunk>();
    var stopwatch = Stopwatch.StartNew();

    await foreach (var chunk in chunker.ChunkAsync(document, cts.Token))
    {
        chunks.Add(chunk);

        // Ensure we're not taking too long per chunk
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            "Individual chunk processing took too long");
    }

    // Assert
    Assert.NotEmpty(chunks);
    Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30),
        "Total chunking took longer than expected");
}

// ✅ Pattern: Testing with realistic async delays and cancellation
[Fact]
public async Task GenerateEmbeddingAsync_WithCancellation_RespondsPromptly()
{
    // Arrange
    var generator = CreateMockEmbeddingGenerator(); // With realistic delays
    var text = "Test text for embedding generation";
    using var cts = new CancellationTokenSource();

    // Act - Start the operation
    var embeddingTask = generator.GenerateEmbeddingAsync(text, cts.Token);

    // Cancel after a short delay
    await Task.Delay(100);
    cts.Cancel();

    // Assert - Should respond to cancellation quickly
    var cancellationTime = Stopwatch.StartNew();
    await Assert.ThrowsAsync<OperationCanceledException>(() => embeddingTask);

    // Verify cancellation was handled promptly (within 1 second)
    Assert.True(cancellationTime.Elapsed < TimeSpan.FromSeconds(1),
        "Cancellation took too long to be processed");
}

// ✅ Pattern: Testing IAsyncEnumerable error handling
[Fact]
public async Task ChunkAsync_WithInvalidInput_HandlesGracefully()
{
    // Arrange
    var chunker = CreateSemanticChunker();
    using var cts = new CancellationTokenSource();

    // Act & Assert - Test various error conditions
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        chunker.ChunkAsync(null, cts.Token).ToListAsync().AsTask());

    await Assert.ThrowsAsync<ArgumentException>(() =>
        chunker.ChunkAsync(string.Empty, cts.Token).ToListAsync().AsTask());
}
```

#### ❌ Common xUnit v3 Testing Mistakes

```csharp
// ❌ Mistake: Not testing cancellation behavior
[Fact]
public async Task ChunkAsync_Test()
{
    var chunks = await chunker.ChunkAsync(document).ToListAsync(); // No cancellation token testing
    Assert.NotEmpty(chunks);
}

// ❌ Mistake: Using .Result in async tests
[Fact]
public void ChunkAsync_WithResult()
{
    var chunks = chunker.ChunkAsync(document).ToListAsync().Result; // Blocks and can deadlock
}

// ❌ Mistake: Not disposing async enumerators
[Fact]
public async Task ChunkAsync_LeaksResources()
{
    var enumerator = chunker.ChunkAsync(document).GetAsyncEnumerator();
    await enumerator.MoveNextAsync();
    // Missing: await enumerator.DisposeAsync();
}

// ❌ Mistake: Over-mocking core algorithms
[Fact]
public void TestWithMocks()
{
    var mockMMR = new Mock<IMaximumMarginalRelevance>();
    // This doesn't test the actual algorithm implementation
}

// ❌ Mistake: No timeout in potentially long-running tests
[Fact]
public async Task ChunkLargeDocument_NoTimeout()
{
    // This could run indefinitely if something goes wrong
    await foreach (var chunk in chunker.ChunkAsync(massiveDocument))
    {
        // Process without any timeout or cancellation
    }
}
```

#### xUnit v3 Best Practices for AIContext Library

1. **Always test cancellation**: Every async method must have cancellation tests
2. **Use realistic timeouts**: Set reasonable timeouts (30s for integration, 5s for unit)
3. **Dispose async enumerators**: Always `await enumerator.DisposeAsync()` in manual enumeration
4. **Test error conditions**: Verify proper exception handling with null/empty inputs
5. **Use `ToListAsync()` carefully**: Good for small collections, avoid for large streams
6. **Mock dependencies, not algorithms**: Mock `IEmbeddingGenerator`, test actual MMR/chunking logic
7. **Test performance characteristics**: Verify operations complete within expected timeframes
8. **Use `using` statements**: Proper resource disposal with `CancellationTokenSource`
9. **Verify async enumerable behavior**: Test that streaming works correctly with cancellation
10. **Integration test patterns**: Test complete workflows with realistic data

## Agent Collaboration Patterns

### When Multiple Agents Work on This Codebase

1. **`ai_working` Folder Coordination** (CRITICAL): Always check and update `ai_working/README.md` for seamless handoffs
2. **State Continuity**: Build upon existing `ai_working` content instead of duplicating research/analysis
3. **Benchmark Coordination**: Only one agent should run benchmarks at a time (they take 2-10+ minutes)
4. **Test Isolation**: Each agent should run full test suite before committing changes
5. **Documentation Updates**: Update both code comments and external docs simultaneously
6. **Version Compatibility**: Always verify .NET Standard 2.1 compatibility for main library

### `ai_working` Folder Collaboration Workflow

**Agent Handoff Best Practices:**
```bash
# 🔄 INCOMING AGENT: Always start here
cat ai_working/README.md 2>/dev/null || echo "Starting fresh - will create README.md"
find ai_working/ -name "*.md" -exec echo "Found: {}" \; 2>/dev/null

# ✅ Review existing work before creating new content
# ✅ Update README.md with your session and current task
# ✅ Reference existing files in your new work

# 🔄 OUTGOING AGENT: Always end here
echo "## Session Complete $(date)" >> ai_working/README.md
echo "**Status**: [Completed/In Progress/Blocked]" >> ai_working/README.md
echo "**Next Steps**: [Specific actions for next agent]" >> ai_working/README.md
```

**Multi-Agent Scenarios:**
- **Research Continuation**: Agent B builds on Agent A's `research/context_analysis.md`
- **Implementation Handoff**: Agent A plans in `plans/`, Agent B implements using those plans
- **Iterative Improvement**: Each agent adds to `analysis/performance_results.md` with new findings
- **Problem Solving**: Use `ai_working/README.md` to communicate blockers and solutions between agents

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

### Async/Await Library Code Pitfalls (CRITICAL FOR LIBRARY DEVELOPMENT)

```csharp
// ❌ Pitfall: Not using ConfigureAwait(false) in library code
public async Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
{
    var response = await httpClient.PostAsync(endpoint, content, cancellationToken); // Missing ConfigureAwait(false)
    var result = await response.Content.ReadAsStringAsync(); // Missing ConfigureAwait(false)
    return ParseEmbedding(result);
}

// ✅ Solution: Always use ConfigureAwait(false) in library code
public async Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
{
    var response = await httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
    var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return ParseEmbedding(result);
}

// ❌ Pitfall: Async void methods (except event handlers)
public async void ProcessChunkAsync(TextChunk chunk) // Never use async void
{
    await embeddingGenerator.GenerateEmbeddingAsync(chunk.Text);
}

// ✅ Solution: Always return Task or Task<T>
public async Task ProcessChunkAsync(TextChunk chunk, CancellationToken cancellationToken = default)
{
    await embeddingGenerator.GenerateEmbeddingAsync(chunk.Text, cancellationToken).ConfigureAwait(false);
}

// ❌ Pitfall: Not handling async enumerable disposal
public async IAsyncEnumerable<TextChunk> ChunkAsync(string text)
{
    var enumerator = textSplitter.SplitAsync(text).GetAsyncEnumerator();

    while (await enumerator.MoveNextAsync()) // Resource leak - no disposal
    {
        yield return await ProcessSegment(enumerator.Current);
    }
    // Missing: await enumerator.DisposeAsync();
}

// ✅ Solution: Proper async enumerable resource management
public async IAsyncEnumerable<TextChunk> ChunkAsync(
    string text,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var segment in textSplitter.SplitAsync(text, cancellationToken).ConfigureAwait(false))
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return await ProcessSegment(segment, cancellationToken).ConfigureAwait(false);
    }
}

// ❌ Pitfall: Mixing sync and async code incorrectly
public List<Vector<double>> GenerateEmbeddings(IEnumerable<string> texts)
{
    return texts.Select(text => embeddingGenerator.GenerateEmbeddingAsync(text).Result).ToList(); // Deadlock risk
}

// ✅ Solution: Fully async or fully sync patterns
public async Task<Vector<double>[]> GenerateEmbeddingsAsync(
    IEnumerable<string> texts,
    CancellationToken cancellationToken = default)
{
    var results = new List<Vector<double>>();

    await foreach (var embedding in embeddingGenerator.GenerateBatchEmbeddingsAsync(texts, cancellationToken).ConfigureAwait(false))
    {
        results.Add(embedding);
    }

    return results.ToArray();
}
```

## Agent-Specific Best Practices

### Code Analysis Tasks

1. **.NET CLI Compliance**: Verify all project operations used `dotnet` CLI (CRITICAL)
2. **Project File Integrity**: Ensure no manual editing of `.slnx`, `.csproj`, or `.sln` files
3. **Performance Analysis**: Always check if new code affects the O(n²k) complexity of MMR
4. **Memory Profiling**: Consider memory usage patterns, especially for streaming operations
5. **Async Correctness**: Verify proper `ConfigureAwait(false)` usage in library code (CRITICAL)
6. **Cancellation Token Analysis**: Ensure all async methods accept and properly propagate cancellation tokens
7. **Resource Disposal**: Verify proper disposal of async enumerators and disposable resources
8. **Deadlock Prevention**: Check for sync-over-async patterns that could cause deadlocks
9. **Vector Operations**: Ensure all similarity calculations use consistent MathNet patterns
10. **Exception Propagation**: Verify exceptions flow correctly through async call chains
11. **Solution Integrity**: Run `dotnet build AiContext.slnx` to verify no build errors
12. **Package Consistency**: Check for version conflicts or unnecessary dependencies

### Documentation Tasks

1. **XML Comments**: Include `<param>`, `<returns>`, `<example>` sections for public APIs
2. **Performance Notes**: Document complexity and memory usage for algorithms
3. **Usage Examples**: Provide realistic examples with actual vector dimensions
4. **Edge Cases**: Document behavior with empty collections, null inputs, extreme lambda values

### Testing Tasks

1. **Edge Case Coverage**: Test with empty vectors, single vectors, lambda extremes (0.0, 1.0)
2. **Performance Regression**: Add benchmark tests for new algorithms
3. **Integration Testing**: Test complete workflows from text input to final results
4. **Cancellation Support**: Verify all async operations respect cancellation tokens (CRITICAL)
5. **xUnit v3 Patterns**: Use proper async testing patterns with timeouts and resource disposal
6. **Async Enumerable Testing**: Test `IAsyncEnumerable` methods with cancellation and error conditions
7. **Timeout Testing**: Verify operations complete within reasonable timeframes
8. **Resource Disposal**: Ensure proper disposal of async enumerators and cancellation token sources
9. **Cancellation Responsiveness**: Test that cancellation is handled promptly (< 1 second)
10. **Error Propagation**: Verify exceptions are properly propagated through async chains

## Microsoft.Extensions.AI Integration (MEAI Project)

The `AiGeekSquad.AIContext.MEAI` project provides seamless integration with Microsoft's AI abstractions:

### Key Integration Points
- **IEmbeddingGenerator compatibility**: Implements Microsoft.Extensions.AI embedding interfaces
- **Dependency injection ready**: Works with standard .NET DI containers
- **Configuration patterns**: Follows Microsoft.Extensions configuration patterns
- **Observability**: Integrates with .NET logging and telemetry

### Working with MEAI
```csharp
// ✅ Correct: Using MEAI integration
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, OpenAIEmbeddingGenerator>();
services.AddSingleton<SemanticTextChunker>();

// Integration with AIContext library
var chunker = serviceProvider.GetService<SemanticTextChunker>();
await foreach (var chunk in chunker.ChunkAsync(document))
{
    // Process semantically chunked content
}
```

### MEAI Best Practices
- **Follow Microsoft.Extensions patterns**: Use standard configuration and DI patterns
- **Respect interface boundaries**: Don't bypass the abstraction layers
- **Test integration points**: Verify compatibility with different embedding providers
- **Document dependencies**: Clear documentation for required Microsoft.Extensions packages

## Working with Examples

The `examples/` directory contains comprehensive demonstrations:

### Available Examples
- **`BasicChunking.cs`**: Core semantic chunking with mock embedding generator
- **`MMRExample.cs`**: Maximum Marginal Relevance algorithm demonstration
- **`ProductSearchDemo.cs`**: E-commerce search with semantic similarity
- **`SupportTicketRouter.cs`**: Customer service ticket routing using MMR
- **`EnterpriseRAGService.cs`**: Complete RAG system implementation

### Example Usage Patterns
```csharp
// ✅ Running specific examples
dotnet run --project examples/ --configuration Release BasicChunking
dotnet run --project examples/ --configuration Release MMR
dotnet run --project examples/ --configuration Release ProductSearch

// ✅ Modifying examples for testing
// Copy example code to test custom scenarios
// Use examples as integration test templates
```

### When Creating New Examples
1. **Follow established patterns**: Use the same structure as existing examples
2. **Include realistic data**: Don't use trivial test cases
3. **Document the scenario**: Clear explanation of the use case
4. **Test thoroughly**: Ensure examples work before committing
5. **Performance considerations**: Include timing information for benchmarking

## Working with Documentation

The `docs/` directory contains comprehensive technical documentation:

### Available Documentation
- **`README.md`**: Documentation hub and overview
- **`MMR.md`**: Detailed MMR algorithm explanation with mathematical foundations
- **`SemanticChunking.md`**: In-depth semantic chunking algorithm documentation
- **`RankingAPI_Architecture.md`**: Generic ranking engine architecture details
- **`RankingAPI_Usage.md`**: Comprehensive usage examples and patterns
- **`PERFORMANCE_TUNING.md`**: Production optimization guidelines
- **`TROUBLESHOOTING.md`**: Common issues and solutions
- **`BenchmarkResults.md`**: Performance analysis and benchmark data

### Documentation Best Practices
1. **Keep docs synchronized**: Update documentation when changing APIs or behavior
2. **Reference specific docs**: Link to relevant documentation sections in code comments
3. **Validate examples**: Ensure all documentation examples compile and run correctly
4. **Performance context**: Include performance characteristics in API documentation
5. **Cross-reference**: Maintain consistency between CLAUDE.md, AGENTS.md, and docs/

### When Updating Documentation
```bash
# Verify documentation examples work
cd docs/
grep -r "```csharp" . | # Find all code examples
# Test each example for compilation and correctness

# Update cross-references
# Ensure CLAUDE.md, AGENTS.md, and docs/ are aligned
```

## Agent Validation Checklist

Before completing any task on this codebase:

### 🗂️ Workspace Management (CRITICAL)
- [ ] **Did you check `ai_working/README.md` before starting work?** (MANDATORY)
- [ ] **Did you update `ai_working/README.md` with your session info and current task?**
- [ ] **Are you building on existing work instead of duplicating effort?**
- [ ] **Did you reference existing `ai_working` files in your new content?**
- [ ] **Did you update `ai_working/README.md` whenever you added/modified/removed files?**
- [ ] **Is there a clear handoff documented for the next agent?**
- [ ] **Are completed items moved to `archive/` with clear naming?**

### 🔧 .NET CLI & Project Management (CRITICAL)
- [ ] **Are you using `dotnet` CLI for ALL project operations?** (MANDATORY)
- [ ] **Did you avoid manually editing `.slnx`, `.csproj`, or `.sln` files?**
- [ ] **Did you use `dotnet add package` instead of manual PackageReference editing?**
- [ ] **Did you use `dotnet add reference` for project references?**
- [ ] **Did you use `dotnet sln add` when adding projects to solution?**
- [ ] **Did you verify solution integrity with `dotnet build AiContext.slnx`?**
- [ ] **Are new projects following the naming convention (AiGeekSquad.AIContext.*)? **
- [ ] **Are target frameworks correct (.NET Standard 2.1 for libs, .NET 9.0 for tests)?**

### 🏗️ Code Quality & Architecture
- [ ] Does the code follow the interface-first pattern?
- [ ] Are all public methods documented with XML comments?
- [ ] Do async methods use `IAsyncEnumerable` where appropriate?
- [ ] Are tests added with >90% coverage?
- [ ] Does `dotnet test` pass with all tests?
- [ ] For performance-critical code, are benchmarks included?
- [ ] Does the code maintain .NET Standard 2.1 compatibility?

### 🔄 Async & Cancellation Patterns (CRITICAL)
- [ ] **Are proper cancellation token patterns used?** (CRITICAL)
- [ ] Do all async methods accept cancellation tokens with `= default`?
- [ ] Are cancellation tokens passed through to nested async calls?
- [ ] Do long-running operations check `ThrowIfCancellationRequested()`?
- [ ] Are `IAsyncEnumerable` methods using `[EnumeratorCancellation]`?
- [ ] Do tests verify cancellation behavior with `OperationCanceledException`?
- [ ] Are async enumerators properly disposed with `await DisposeAsync()`?
- [ ] Do tests use appropriate timeouts (30s integration, 5s unit)?
- [ ] Is cancellation responsiveness tested (< 1 second)?
- [ ] Is `ConfigureAwait(false)` used in all library code?

### 🚀 Performance & Integration
- [ ] Does the implementation respect the O(n²k) performance characteristics?
- [ ] Is documentation updated and cross-referenced correctly?
- [ ] Are examples tested and functional?
- [ ] Is MEAI integration compatibility maintained?

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

- **GitHub Actions**: Automated builds, testing, SonarQube analysis, and NuGet publishing on main branch
- **SonarQube Integration**: Ensure code coverage reports are generated correctly using Cobertura and OpenCover formats
- **Windows Compatibility**: Test with `dotnet build AiContext.slnx --configuration Release`
- **NuGet Packaging**: Automated packaging and publishing to NuGet.org from main branch

This guidance should help AI agents work effectively with the AIContext library while maintaining code quality, performance standards, and architectural consistency.