using BenchmarkDotNet.Running;
using AiGeekSquad.AIContext.Benchmarks;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Entry point for running MMR algorithm benchmarks
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== MMR Algorithm Benchmarks ===");
        Console.WriteLine("This will run comprehensive performance benchmarks for the Maximum Marginal Relevance algorithm.");
        Console.WriteLine("The benchmarks will test various combinations of:");
        Console.WriteLine("- Vector counts: 100, 1000, 5000");
        Console.WriteLine("- Vector dimensions: 10, 100, 500");
        Console.WriteLine("- TopK values: 5, 10, 20");
        Console.WriteLine("- Lambda values: 0.0, 0.5, 1.0");
        Console.WriteLine();
        Console.WriteLine("Results will be exported to multiple formats (Markdown, CSV, HTML).");
        Console.WriteLine("Press any key to start benchmarking...");
        Console.ReadKey();
        Console.WriteLine();

        // Run the benchmarks
        var summary = BenchmarkRunner.Run<MmrBenchmarks>();

        Console.WriteLine();
        Console.WriteLine("=== Benchmark Summary ===");
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
        Console.WriteLine("Benchmarking complete! Check the results directory for detailed reports.");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}