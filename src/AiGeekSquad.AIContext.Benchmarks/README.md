# MMR Algorithm Benchmarks

This project contains comprehensive performance benchmarks for the Maximum Marginal Relevance (MMR) algorithm implementation using BenchmarkDotNet.

## Overview

The benchmarks test the MMR algorithm performance across various parameter combinations to understand its computational complexity and memory usage patterns.

## Benchmark Parameters

The benchmarks test the following parameter combinations:

- **Vector counts**: 100, 1000, 5000 vectors
- **Vector dimensions**: 10, 100, 500 dimensions
- **TopK values**: 5, 10, 20 results
- **Lambda values**: 0.0 (pure diversity), 0.5 (balanced), 1.0 (pure relevance)

## Benchmark Types

### Main Benchmark (`ComputeMMR`)
- Tests all parameter combinations using `[Params]` attributes
- Provides comprehensive performance analysis

### Specialized Benchmarks
- `ComputeMMR_PureRelevance`: Tests with lambda = 1.0 (relevance only)
- `ComputeMMR_PureDiversity`: Tests with lambda = 0.0 (diversity only)
- `ComputeMMR_Balanced`: Tests with lambda = 0.5 (balanced approach)
- `ComputeMMR_SmallTopK`: Tests with TopK = 5
- `ComputeMMR_MediumTopK`: Tests with TopK = 10
- `ComputeMMR_LargeTopK`: Tests with TopK = 20
- `ComputeMMR_MemoryFocused`: Specialized for memory allocation analysis

## Running the Benchmarks

### Prerequisites
- .NET 9.0 SDK
- Windows (for Windows-specific performance counters)

### Command Line
```bash
# Build the project
dotnet build src/AiGeekSquad.AIContext.Benchmarks/

# Run all benchmarks
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release

# Run specific benchmark method
dotnet run --project src/AiGeekSquad.AIContext.Benchmarks/ --configuration Release -- --filter "*ComputeMMR_Balanced*"
```

### Interactive Mode
```bash
# Run the executable directly
cd src/AiGeekSquad.AIContext.Benchmarks/bin/Release/net9.0/
./AiGeekSquad.AIContext.Benchmarks.exe
```

## Configuration

The benchmarks use a custom configuration (`BenchmarkConfig.cs`) that includes:

- **Multiple GC modes**: Server GC and Workstation GC
- **Memory diagnostics**: Allocation tracking and memory usage
- **Multiple export formats**: Markdown, HTML
- **Statistical analysis**: Mean, Median, P95, Rankings

## Output

Benchmark results are exported to:
- **Console**: Real-time progress and summary
- **Markdown**: GitHub-compatible tables
- **HTML**: Interactive web report
- **BenchmarkDotNet artifacts**: Detailed logs and data

## Performance Insights

The benchmarks help identify:
- **Scalability**: How performance scales with vector count and dimensions
- **Memory usage**: Allocation patterns and memory efficiency
- **Parameter impact**: How lambda and TopK values affect performance
- **GC behavior**: Impact of different garbage collection strategies

## Test Data Generation

- Uses fixed seed (42) for reproducible results
- Generates random vectors with values between -1 and 1
- Creates fresh test data for each benchmark iteration to avoid caching effects

## Migrated from Unit Tests

The performance test `ComputeMMR_WithLargerDataset_PerformsCorrectly` has been migrated from the unit test project to this dedicated benchmark project for more comprehensive performance analysis.