using AiGeekSquad.AIContext.Ranking;
using AiGeekSquad.AIContext.Ranking.Normalizers;
using AiGeekSquad.AIContext.Ranking.Strategies;
using BenchmarkDotNet.Attributes;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Comprehensive benchmarks for the Ranking Engine performance testing
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(baseline: true)]
public class RankingEngineBenchmarks
{
    private RankingEngine<BenchmarkItem> _engine = null!;
    private List<BenchmarkItem> _smallDataset = null!;
    private List<BenchmarkItem> _mediumDataset = null!;
    private List<BenchmarkItem> _largeDataset = null!;
    private Random _random = null!;

    // Scoring functions
    private WeightedScoringFunction<BenchmarkItem> _simpleScoringFunction = null!;
    private WeightedScoringFunction<BenchmarkItem> _complexScoringFunction = null!;
    private WeightedScoringFunction<BenchmarkItem> _expensiveScoringFunction = null!;
    private WeightedScoringFunction<BenchmarkItem> _dissimilarityScoringFunction = null!;

    // Multiple scoring function combinations
    private List<WeightedScoringFunction<BenchmarkItem>> _multipleScoringFunctions = null!;
    private List<WeightedScoringFunction<BenchmarkItem>> _mixedWeightScoringFunctions = null!;
    private List<WeightedScoringFunction<BenchmarkItem>> _similarityDissimilarityMix = null!;

    // Normalizers
    private MinMaxNormalizer _minMaxNormalizer = null!;
    private ZScoreNormalizer _zScoreNormalizer = null!;
    private PercentileNormalizer _percentileNormalizer = null!;

    // Strategies
    private WeightedSumStrategy _weightedSumStrategy = null!;
    private ReciprocalRankFusionStrategy _rrfStrategy = null!;
    private HybridStrategy _hybridStrategy = null!;

    [Params(100, 1000, 5000)]
    public int DatasetSize { get; set; }

    [Params(10, 50)]
    public int TopK { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _random = new Random(42); // Fixed seed for reproducibility

        // Initialize normalizers
        _minMaxNormalizer = new MinMaxNormalizer();
        _zScoreNormalizer = new ZScoreNormalizer();
        _percentileNormalizer = new PercentileNormalizer();

        // Initialize strategies
        _weightedSumStrategy = new WeightedSumStrategy();
        _rrfStrategy = new ReciprocalRankFusionStrategy();
        _hybridStrategy = new HybridStrategy();

        // Initialize engine
        _engine = new RankingEngine<BenchmarkItem>();

        // Generate datasets
        _smallDataset = GenerateDataset(100);
        _mediumDataset = GenerateDataset(1000);
        _largeDataset = GenerateDataset(5000);

        // Initialize scoring functions
        InitializeScoringFunctions();
    }

    private void InitializeScoringFunctions()
    {
        // Simple scoring function (similarity)
        _simpleScoringFunction = new WeightedScoringFunction<BenchmarkItem>(
            new SimpleScoringFunction(), 1.0)
        { Normalizer = _minMaxNormalizer };

        // Complex scoring function with multiple calculations
        _complexScoringFunction = new WeightedScoringFunction<BenchmarkItem>(
            new ComplexScoringFunction(), 1.0)
        { Normalizer = _minMaxNormalizer };

        // Expensive scoring function (simulates heavy computation)
        _expensiveScoringFunction = new WeightedScoringFunction<BenchmarkItem>(
            new ExpensiveScoringFunction(), 1.0)
        { Normalizer = _minMaxNormalizer };

        // Dissimilarity scoring function (negative weight)
        _dissimilarityScoringFunction = new WeightedScoringFunction<BenchmarkItem>(
            new DissimilarityScoringFunction(), -0.5)
        { Normalizer = _minMaxNormalizer };

        // Multiple scoring functions (3-5 functions)
        _multipleScoringFunctions = new List<WeightedScoringFunction<BenchmarkItem>>
        {
            new(new SimpleScoringFunction("Relevance"), 1.0) { Normalizer = _minMaxNormalizer },
            new(new PopularityScoringFunction(), 0.8) { Normalizer = _minMaxNormalizer },
            new(new RecencyScoringFunction(), 0.6) { Normalizer = _minMaxNormalizer },
            new(new QualityScoringFunction(), 0.9) { Normalizer = _minMaxNormalizer },
            new(new AuthorityScoringFunction(), 0.7) { Normalizer = _minMaxNormalizer }
        };

        // Mixed weight scoring functions (positive and negative weights)
        _mixedWeightScoringFunctions = new List<WeightedScoringFunction<BenchmarkItem>>
        {
            new(new SimpleScoringFunction("Similarity"), 1.0) { Normalizer = _minMaxNormalizer },
            new(new DissimilarityScoringFunction(), -0.3) { Normalizer = _minMaxNormalizer },
            new(new PopularityScoringFunction(), 0.5) { Normalizer = _minMaxNormalizer }
        };

        // Similarity + Dissimilarity mix
        _similarityDissimilarityMix = new List<WeightedScoringFunction<BenchmarkItem>>
        {
            new(new SimpleScoringFunction("Primary"), 1.0) { Normalizer = _minMaxNormalizer },
            new(new SimpleScoringFunction("Secondary"), 0.8) { Normalizer = _minMaxNormalizer },
            new(new DissimilarityScoringFunction("Noise"), -0.4) { Normalizer = _minMaxNormalizer },
            new(new DissimilarityScoringFunction("Spam"), -0.6) { Normalizer = _minMaxNormalizer }
        };
    }

    private List<BenchmarkItem> GenerateDataset(int size)
    {
        var items = new List<BenchmarkItem>(size);
        for (int i = 0; i < size; i++)
        {
            items.Add(new BenchmarkItem
            {
                Id = i,
                Value = _random.NextDouble(),
                Popularity = _random.Next(1, 1000),
                Quality = _random.NextDouble(),
                Age = _random.Next(1, 365),
                Authority = _random.NextDouble(),
                Category = _random.Next(1, 10)
            });
        }
        return items;
    }

    private List<BenchmarkItem> GetDatasetBySize()
    {
        return DatasetSize switch
        {
            100 => _smallDataset,
            1000 => _mediumDataset,
            5000 => _largeDataset,
            _ => _smallDataset
        };
    }

    // BASELINE BENCHMARKS

    /// <summary>
    /// Baseline benchmark: Single scoring function with WeightedSum strategy
    /// </summary>
    [Benchmark(Baseline = true)]
    public IList<RankedResult<BenchmarkItem>> Baseline_SingleFunction_WeightedSum()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, new[] { _simpleScoringFunction }, _weightedSumStrategy);
    }

    /// <summary>
    /// Baseline benchmark: Single scoring function with Top-K ranking
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Baseline_SingleFunction_TopK()
    {
        var dataset = GetDatasetBySize();
        return _engine.RankTopK(dataset, new[] { _simpleScoringFunction }, TopK, _weightedSumStrategy);
    }

    // MULTIPLE SCORING FUNCTIONS BENCHMARKS

    /// <summary>
    /// Benchmark: Multiple scoring functions (3-5) with mixed weights
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> MultipleFunctions_MixedWeights()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _multipleScoringFunctions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Multiple scoring functions with Top-K
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> MultipleFunctions_TopK()
    {
        var dataset = GetDatasetBySize();
        return _engine.RankTopK(dataset, _multipleScoringFunctions, TopK, _weightedSumStrategy);
    }

    // NORMALIZATION STRATEGIES BENCHMARKS

    /// <summary>
    /// Benchmark: MinMax normalization with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Normalization_MinMax()
    {
        var dataset = GetDatasetBySize();
        var functions = _multipleScoringFunctions.Select(f =>
            new WeightedScoringFunction<BenchmarkItem>(f.Function, f.Weight) { Normalizer = _minMaxNormalizer }).ToList();
        return _engine.Rank(dataset, functions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Z-Score normalization with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Normalization_ZScore()
    {
        var dataset = GetDatasetBySize();
        var functions = _multipleScoringFunctions.Select(f =>
            new WeightedScoringFunction<BenchmarkItem>(f.Function, f.Weight) { Normalizer = _zScoreNormalizer }).ToList();
        return _engine.Rank(dataset, functions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Percentile normalization with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Normalization_Percentile()
    {
        var dataset = GetDatasetBySize();
        var functions = _multipleScoringFunctions.Select(f =>
            new WeightedScoringFunction<BenchmarkItem>(f.Function, f.Weight) { Normalizer = _percentileNormalizer }).ToList();
        return _engine.Rank(dataset, functions, _weightedSumStrategy);
    }

    // COMBINATION STRATEGIES BENCHMARKS

    /// <summary>
    /// Benchmark: WeightedSum strategy with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Strategy_WeightedSum()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _multipleScoringFunctions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Reciprocal Rank Fusion (RRF) strategy
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Strategy_RRF()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _multipleScoringFunctions, _rrfStrategy);
    }

    /// <summary>
    /// Benchmark: Hybrid strategy combining multiple approaches
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Strategy_Hybrid()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _multipleScoringFunctions, _hybridStrategy);
    }

    // SIMILARITY VS DISSIMILARITY BENCHMARKS

    /// <summary>
    /// Benchmark: Similarity-only scoring functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> SimilarityOnly()
    {
        var dataset = GetDatasetBySize();
        var similarityFunctions = _multipleScoringFunctions.Take(3).ToList();
        return _engine.Rank(dataset, similarityFunctions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Mixed similarity and dissimilarity scoring
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> SimilarityDissimilarityMix()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _similarityDissimilarityMix, _weightedSumStrategy);
    }

    // COMPUTATIONAL COMPLEXITY BENCHMARKS

    /// <summary>
    /// Benchmark: Simple scoring function (low computational cost)
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Complexity_Simple()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, new[] { _simpleScoringFunction }, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Complex scoring function (medium computational cost)
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Complexity_Complex()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, new[] { _complexScoringFunction }, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Expensive scoring function (high computational cost)
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> Complexity_Expensive()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, new[] { _expensiveScoringFunction }, _weightedSumStrategy);
    }

    // TOP-K VS FULL RANKING BENCHMARKS

    /// <summary>
    /// Benchmark: Full ranking with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> FullRanking_MultipleFunctions()
    {
        var dataset = GetDatasetBySize();
        return _engine.Rank(dataset, _multipleScoringFunctions, _weightedSumStrategy);
    }

    /// <summary>
    /// Benchmark: Top-K ranking with multiple functions
    /// </summary>
    [Benchmark]
    public IList<RankedResult<BenchmarkItem>> TopKRanking_MultipleFunctions()
    {
        var dataset = GetDatasetBySize();
        return _engine.RankTopK(dataset, _multipleScoringFunctions, TopK, _weightedSumStrategy);
    }
}

/// <summary>
/// Benchmark item for testing ranking performance
/// </summary>
public class BenchmarkItem
{
    public int Id { get; set; }
    public double Value { get; set; }
    public int Popularity { get; set; }
    public double Quality { get; set; }
    public int Age { get; set; }
    public double Authority { get; set; }
    public int Category { get; set; }

    public override string ToString() => $"Item {Id} (Value: {Value:F2})";
}

// MOCK SCORING FUNCTIONS FOR BENCHMARKING

/// <summary>
/// Simple scoring function with minimal computation
/// </summary>
public class SimpleScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; }

    public SimpleScoringFunction(string name = "Simple")
    {
        Name = name;
    }

    public double ComputeScore(BenchmarkItem item) => item.Value;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(item => item.Value).ToArray();
    }
}

/// <summary>
/// Complex scoring function with multiple calculations
/// </summary>
public class ComplexScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Complex";

    public double ComputeScore(BenchmarkItem item)
    {
        // Simulate complex calculation
        var baseScore = item.Value;
        var qualityBoost = Math.Log(1 + item.Quality);
        var popularityFactor = Math.Sqrt(item.Popularity / 1000.0);
        var agePenalty = Math.Exp(-item.Age / 100.0);

        return baseScore * qualityBoost * popularityFactor * agePenalty;
    }

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(ComputeScore).ToArray();
    }
}

/// <summary>
/// Expensive scoring function simulating heavy computation
/// </summary>
public class ExpensiveScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Expensive";

    public double ComputeScore(BenchmarkItem item)
    {
        // Simulate expensive computation with Thread.Sleep
        Thread.Sleep(1); // 1ms delay per item

        // Complex mathematical operations
        var result = item.Value;
        for (int i = 0; i < 100; i++)
        {
            result = Math.Sin(result) * Math.Cos(item.Quality) + Math.Sqrt(item.Popularity);
        }

        return Math.Abs(result);
    }

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(ComputeScore).ToArray();
    }
}

/// <summary>
/// Dissimilarity scoring function (returns inverse scores)
/// </summary>
public class DissimilarityScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; }

    public DissimilarityScoringFunction(string name = "Dissimilarity")
    {
        Name = name;
    }

    public double ComputeScore(BenchmarkItem item) => 1.0 - item.Value;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(item => 1.0 - item.Value).ToArray();
    }
}

/// <summary>
/// Popularity-based scoring function
/// </summary>
public class PopularityScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Popularity";

    public double ComputeScore(BenchmarkItem item) => 1.0 / item.Popularity;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(item => 1.0 / item.Popularity).ToArray();
    }
}

/// <summary>
/// Recency-based scoring function
/// </summary>
public class RecencyScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Recency";

    public double ComputeScore(BenchmarkItem item) => Math.Max(0, 365 - item.Age) / 365.0;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(ComputeScore).ToArray();
    }
}

/// <summary>
/// Quality-based scoring function
/// </summary>
public class QualityScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Quality";

    public double ComputeScore(BenchmarkItem item) => item.Quality;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(item => item.Quality).ToArray();
    }
}

/// <summary>
/// Authority-based scoring function
/// </summary>
public class AuthorityScoringFunction : IScoringFunction<BenchmarkItem>
{
    public string Name { get; } = "Authority";

    public double ComputeScore(BenchmarkItem item) => item.Authority;

    public double[] ComputeScores(IReadOnlyList<BenchmarkItem> items)
    {
        return items.Select(item => item.Authority).ToArray();
    }
}