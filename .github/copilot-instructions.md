# AIContext Library - Copilot Instructions

Always follow these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

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

## Dependencies and Packaging

### Key Dependencies
- **MathNet.Numerics v5.0.0** - Vector operations and similarity calculations
- **Microsoft.ML.Tokenizers v1.0.2** - Accurate token counting (GPT-4 compatible)
- **Markdig v0.41.3** - Markdown processing

### NuGet Package
- **Package ID**: `AiGeekSquad.AIContext`
- **Target**: .NET Standard 2.1 for broad compatibility
- **Build for packaging**: `dotnet pack AiContext.slnx --configuration Release --no-build --output packages`

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