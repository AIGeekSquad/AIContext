using System.Runtime.CompilerServices;
using AiGeekSquad.AIContext.Chunking;
using BenchmarkDotNet.Attributes;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Comprehensive benchmarks for the Semantic Text Chunking functionality
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(baseline: true)]
public class SemanticChunkingBenchmarks
{
    #region Mock Implementations for Benchmarking

    /// <summary>
    /// High-performance mock implementation of ITokenCounter for benchmarking
    /// </summary>
    private class BenchmarkTokenCounter : ITokenCounter
    {
        private const double AverageTokensPerWord = 1.3; // Rough approximation for English text

        public int CountTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Fast approximation: split by whitespace and multiply by average tokens per word
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return (int)(wordCount * AverageTokensPerWord);
        }

        public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CountTokens(text));
        }
    }

    /// <summary>
    /// High-performance mock implementation of IEmbeddingGenerator for benchmarking
    /// </summary>
    private class BenchmarkEmbeddingGenerator(int dimensions = 384) : IEmbeddingGenerator
    {
        private readonly Random _random = new(42); // Fixed seed for reproducibility

        public Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateFastEmbedding(text));
        }

        public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
            IEnumerable<string> texts,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var text in texts)
            {
                yield return CreateFastEmbedding(text);
                await Task.Yield(); // Simulate minimal async behavior
            }
        }

        private Vector<double> CreateFastEmbedding(string text)
        {
            var values = new double[dimensions];
            var hash = text.GetHashCode();
            var localRandom = new Random(Math.Abs(hash));

            // Generate normalized random vector
            for (var i = 0; i < dimensions; i++)
            {
                values[i] = localRandom.NextDouble() * 2.0 - 1.0;
            }

            // Fast normalization
            var sumSquares = values.Sum(v => v * v);
            var magnitude = Math.Sqrt(sumSquares);
            if (magnitude > 0)
            {
                for (var i = 0; i < dimensions; i++)
                    values[i] /= magnitude;
            }

            return Vector<double>.Build.DenseOfArray(values);
        }
    }

    #endregion

    #region Test Data Generation

    private readonly string[] _shortTexts =
    [
        "Technology drives innovation.",
        "Software development evolves rapidly.",
        "AI research advances continuously.",
        "Business strategies adapt to change.",
        "Market conditions influence decisions."
    ];

    private readonly string[] _mediumTexts =
    [
        "Technology has revolutionized the way we live and work in the modern era. " +
        "Artificial intelligence and machine learning are transforming industries across the globe. " +
        "Software development practices continue to evolve with new frameworks and methodologies.",

        "Business leaders must adapt to rapid technological changes to remain competitive. " +
        "Companies are investing heavily in digital transformation initiatives. " +
        "Market conditions favor organizations that embrace technological innovation.",

        "Scientific research drives innovation in countless fields of human endeavor. " +
        "Computer science research enables breakthrough discoveries and applications. " +
        "Academic institutions collaborate with industry to advance knowledge and technology."
    ];

    private readonly string[] _longTexts =
    [
        "Technology has fundamentally transformed every aspect of human civilization over the past century. " +
        "From the invention of the computer to the development of artificial intelligence, we have witnessed " +
        "unprecedented changes in how we work, communicate, and solve complex problems. Software development " +
        "has emerged as one of the most critical skills of the 21st century, enabling innovations across " +
        "healthcare, finance, education, and entertainment industries. Machine learning algorithms now power " +
        "recommendation systems, autonomous vehicles, and medical diagnostic tools that save lives daily. " +
        "The rapid evolution of programming languages, frameworks, and development methodologies continues " +
        "to accelerate the pace of innovation. Cloud computing has democratized access to powerful computing " +
        "resources, allowing startups and enterprises alike to scale their solutions globally.",

        "Business strategy in the digital age requires a fundamental understanding of technological trends " +
        "and their implications for market dynamics. Companies that fail to embrace digital transformation " +
        "risk becoming obsolete in an increasingly competitive landscape. Market leaders leverage data " +
        "analytics, artificial intelligence, and automation to optimize operations and enhance customer " +
        "experiences. Strategic decision-making now relies heavily on real-time data insights and predictive " +
        "modeling capabilities. Organizations must balance innovation with risk management while maintaining " +
        "regulatory compliance and ethical standards. The emergence of new business models enabled by " +
        "technology platforms has disrupted traditional industries and created entirely new market segments.",

        "Scientific research methodology has been revolutionized by computational tools and data analysis " +
        "techniques that enable researchers to process vast amounts of information quickly and accurately. " +
        "Interdisciplinary collaboration between computer scientists, domain experts, and data analysts " +
        "has accelerated breakthrough discoveries in fields ranging from genomics to climate science. " +
        "Research institutions worldwide are investing in high-performance computing infrastructure " +
        "to support complex simulations and modeling efforts. Open-source software and collaborative " +
        "platforms have democratized access to advanced research tools and methodologies. The integration " +
        "of artificial intelligence into research workflows has automated many routine tasks and enabled " +
        "researchers to focus on higher-level analysis and interpretation of results."
    ];

    private string _currentText = string.Empty;
    private SemanticTextChunker _chunker = null!;
    private SemanticChunkingOptions _currentOptions = null!;

    #endregion

    #region Benchmark Parameters

    [Params(TextSize.Short, TextSize.Medium, TextSize.Long)]
    public TextSize DocumentSize { get; set; }

    [Params(256, 512)]
    public int MaxTokensPerChunk { get; set; }

    [Params(true, false)]
    public bool EnableCaching { get; set; }

    public enum TextSize
    {
        Short,
        Medium,
        Long
    }

    #endregion

    #region Setup Methods

    [GlobalSetup]
    public void GlobalSetup()
    {
        var tokenCounter = new BenchmarkTokenCounter();
        var embeddingGenerator = new BenchmarkEmbeddingGenerator();
        _chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Select text based on size parameter
        _currentText = DocumentSize switch
        {
            TextSize.Short => string.Join(" ", _shortTexts),
            TextSize.Medium => string.Join(" ", _mediumTexts),
            TextSize.Long => string.Join(" ", _longTexts),
            _ => string.Join(" ", _mediumTexts)
        };

        // Configure options based on parameters with reasonable defaults
        _currentOptions = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = MaxTokensPerChunk,
            MinTokensPerChunk = Math.Max(10, MaxTokensPerChunk / 10),
            BufferSize = 2, // Fixed reasonable buffer size
            BreakpointPercentileThreshold = 0.80, // Fixed reasonable threshold
            EnableEmbeddingCaching = EnableCaching,
            MaxCacheSize = 1000
        };
    }

    #endregion

    #region Core Chunking Benchmarks

    /// <summary>
    /// Benchmark overall chunking performance with various parameter combinations
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<List<TextChunk>> SemanticChunking_Complete()
    {
        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, _currentOptions))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    /// <summary>
    /// Benchmark chunking with default options
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_DefaultOptions()
    {
        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, SemanticChunkingOptions.Default))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    /// <summary>
    /// Benchmark chunking with optimized options for speed
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_OptimizedForSpeed()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BufferSize = 1, // Minimal buffer for speed
            BreakpointPercentileThreshold = 0.75,
            EnableEmbeddingCaching = true,
            MaxCacheSize = 1000
        };

        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    /// <summary>
    /// Benchmark chunking with optimized options for quality
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_OptimizedForQuality()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 1024,
            MinTokensPerChunk = 100,
            BufferSize = 3, // Larger buffer for better context
            BreakpointPercentileThreshold = 0.90, // Stricter threshold
            EnableEmbeddingCaching = true,
            MaxCacheSize = 1000
        };

        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    #endregion

    #region Configuration Impact Benchmarks

    /// <summary>
    /// Benchmark the impact of different buffer sizes
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_SmallBuffer()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BufferSize = 1,
            BreakpointPercentileThreshold = 0.80,
            EnableEmbeddingCaching = EnableCaching
        };

        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    /// <summary>
    /// Benchmark the impact of different buffer sizes
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_LargeBuffer()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BufferSize = 4,
            BreakpointPercentileThreshold = 0.80,
            EnableEmbeddingCaching = EnableCaching
        };

        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    #endregion

    #region Caching Performance Benchmarks

    /// <summary>
    /// Benchmark embedding caching performance - first pass (cache misses)
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_CachingFirstPass()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BufferSize = 2,
            BreakpointPercentileThreshold = 0.75,
            EnableEmbeddingCaching = true,
            MaxCacheSize = 1000
        };

        // Create a new chunker to ensure empty cache
        var tokenCounter = new BenchmarkTokenCounter();
        var embeddingGenerator = new BenchmarkEmbeddingGenerator();
        var freshChunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);

        var chunks = new List<TextChunk>();
        await foreach (var chunk in freshChunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    /// <summary>
    /// Benchmark without caching for comparison
    /// </summary>
    [Benchmark]
    public async Task<List<TextChunk>> SemanticChunking_NoCaching()
    {
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 512,
            MinTokensPerChunk = 50,
            BufferSize = 2,
            BreakpointPercentileThreshold = 0.75,
            EnableEmbeddingCaching = false
        };

        var chunks = new List<TextChunk>();
        await foreach (var chunk in _chunker.ChunkAsync(_currentText, options))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    #endregion
}