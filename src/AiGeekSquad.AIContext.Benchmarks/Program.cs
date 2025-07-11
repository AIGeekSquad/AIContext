using BenchmarkDotNet.Running;
using AiGeekSquad.AIContext.Benchmarks;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Entry point for running AI Context library benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== AI Context Library Benchmarks ===");
        
        // Parse command line arguments
        var benchmarkType = args.Length > 0 ? args[0].ToLowerInvariant() : "mmr";
        
        switch (benchmarkType)
        {
            case "mmr":
                RunMmrBenchmarks();
                break;
            case "semantic":
            case "chunking":
                RunSemanticChunkingBenchmarks();
                break;
            case "all":
                RunAllBenchmarks();
                break;
            default:
                Console.WriteLine($"Unknown benchmark type '{benchmarkType}'. Available options: mmr, semantic, all");
                Console.WriteLine("Running MMR benchmarks by default...");
                RunMmrBenchmarks();
                break;
        }
    }

    private static void RunMmrBenchmarks()
    {
        Console.WriteLine("=== MMR Algorithm Benchmarks ===");
        Console.WriteLine("Running comprehensive performance benchmarks for the Maximum Marginal Relevance algorithm.");
        Console.WriteLine("Testing various combinations of:");
        Console.WriteLine("- Vector counts: 100, 1000, 5000");
        Console.WriteLine("- Vector dimensions: 10, 100, 500");
        Console.WriteLine("- TopK values: 5, 10, 20");
        Console.WriteLine("- Lambda values: 0.0, 0.5, 1.0");
        Console.WriteLine();

        var summary = BenchmarkRunner.Run<MmrBenchmarks>();
        DisplaySummary(summary, "MMR");
    }

    private static void RunSemanticChunkingBenchmarks()
    {
        Console.WriteLine("=== Semantic Chunking Benchmarks ===");
        Console.WriteLine("Running comprehensive performance benchmarks for the Semantic Text Chunking functionality.");
        Console.WriteLine("Testing various combinations of:");
        Console.WriteLine("- Document sizes: Short, Medium, Long");
        Console.WriteLine("- Max tokens per chunk: 128, 256, 512, 1024");
        Console.WriteLine("- Buffer sizes: 1, 2, 3");
        Console.WriteLine("- Breakpoint thresholds: 0.75, 0.85, 0.95");
        Console.WriteLine("- Caching enabled/disabled");
        Console.WriteLine();

        var summary = BenchmarkRunner.Run<SemanticChunkingBenchmarks>();
        DisplaySummary(summary, "Semantic Chunking");
    }

    private static void RunAllBenchmarks()
    {
        Console.WriteLine("=== Running All Benchmarks ===");
        Console.WriteLine("This will run both MMR and Semantic Chunking benchmarks sequentially.");
        Console.WriteLine();

        RunMmrBenchmarks();
        Console.WriteLine();
        Console.WriteLine("Moving to Semantic Chunking benchmarks...");
        Console.WriteLine();
        RunSemanticChunkingBenchmarks();
    }

    private static void DisplaySummary(BenchmarkDotNet.Reports.Summary summary, string benchmarkName)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {benchmarkName} Benchmark Summary ===");
        Console.WriteLine($"Total benchmarks run: {summary.Reports.Count()}");
        Console.WriteLine($"Total errors: {summary.Reports.Count(r => !r.Success)}");
        Console.WriteLine($"Results location: {summary.ResultsDirectoryPath}");
        
        if (summary.Reports.Any(r => !r.Success))
        {
            Console.WriteLine();
            Console.WriteLine("=== Errors ===");
            foreach (var report in summary.Reports.Where(r => !r.Success))
            {
                Console.WriteLine($"- {report.BenchmarkCase.DisplayInfo}: {report.ExecuteResults.FirstOrDefault()?.ExitCode}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{benchmarkName} benchmarking complete! Check the results directory for detailed reports.");
    }
}