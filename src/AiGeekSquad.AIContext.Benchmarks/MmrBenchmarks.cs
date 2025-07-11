using AiGeekSquad.AIContext.Ranking;

using BenchmarkDotNet.Attributes;

using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Comprehensive benchmarks for the Maximum Marginal Relevance (MMR) algorithm
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[SimpleJob(baseline: true)]
public class MmrBenchmarks
{
    private List<Vector<double>> _vectors = null!;
    private Vector<double> _query = null!;
    private Random _random = null!;

    [Params(1000)]
    public int VectorCount { get; set; }

    [Params(100, 384)]
    public int VectorDimension { get; set; }

    [Params(10)]
    public int TopK { get; set; }

    [Params(0.0, 0.5, 1.0)]
    public double Lambda { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _random = new Random(42); // Fixed seed for reproducibility
        GenerateTestData();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Regenerate data for each iteration to avoid caching effects
        GenerateTestData();
    }

    /// <summary>
    /// Generate random test vectors and query for benchmarking
    /// </summary>
    private void GenerateTestData()
    {
        _vectors = new List<Vector<double>>(VectorCount);

        // Generate random vectors
        for (int i = 0; i < VectorCount; i++)
        {
            var values = new double[VectorDimension];
            for (int j = 0; j < VectorDimension; j++)
            {
                values[j] = _random.NextDouble() * 2 - 1; // Random values between -1 and 1
            }
            _vectors.Add(Vector<double>.Build.DenseOfArray(values));
        }

        // Generate random query vector
        var queryValues = new double[VectorDimension];
        for (int i = 0; i < VectorDimension; i++)
        {
            queryValues[i] = _random.NextDouble() * 2 - 1;
        }
        _query = Vector<double>.Build.DenseOfArray(queryValues);
    }

    /// <summary>
    /// Benchmark the MMR algorithm with various parameter combinations
    /// </summary>
    [Benchmark]
    public List<(int index, Vector<double> embedding)> ComputeMMR()
    {
        return MaximumMarginalRelevance.ComputeMMR(_vectors, _query, Lambda, TopK);
    }

    /// <summary>
    /// Benchmark pure relevance selection (lambda = 1.0)
    /// </summary>
    [Benchmark]
    public List<(int index, Vector<double> embedding)> ComputeMMR_PureRelevance()
    {
        return MaximumMarginalRelevance.ComputeMMR(_vectors, _query, lambda: 1.0, TopK);
    }

    /// <summary>
    /// Benchmark pure diversity selection (lambda = 0.0)
    /// </summary>
    [Benchmark]
    public List<(int index, Vector<double> embedding)> ComputeMMR_PureDiversity()
    {
        return MaximumMarginalRelevance.ComputeMMR(_vectors, _query, lambda: 0.0, TopK);
    }

    /// <summary>
    /// Benchmark balanced selection (lambda = 0.5)
    /// </summary>
    [Benchmark]
    public List<(int index, Vector<double> embedding)> ComputeMMR_Balanced()
    {
        return MaximumMarginalRelevance.ComputeMMR(_vectors, _query, lambda: 0.5, TopK);
    }

    /// <summary>
    /// Benchmark memory allocation patterns
    /// </summary>
    [Benchmark]
    public List<(int index, Vector<double> embedding)> ComputeMMR_MemoryFocused()
    {
        // This benchmark is specifically for memory allocation analysis
        var result = MaximumMarginalRelevance.ComputeMMR(_vectors, _query, Lambda, TopK);

        // Force garbage collection to measure true allocation
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return result;
    }
}