using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

namespace AiGeekSquad.AIContext.Benchmarks;

/// <summary>
/// Custom benchmark configuration for MMR algorithm performance testing
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        // Add multiple runtime configurations
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithPlatform(Platform.X64)
            .WithGcServer(true)
            .WithGcConcurrent(true)
            .WithGcRetainVm(true)
            .WithId("ServerGC"));

        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithPlatform(Platform.X64)
            .WithGcServer(false)
            .WithGcConcurrent(true)
            .WithId("WorkstationGC"));

        // Add memory diagnoser to track allocations
        AddDiagnoser(MemoryDiagnoser.Default);

        // Add useful columns for analysis
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.P95);
        AddColumn(RankColumn.Arabic);

        // Add exporters for different output formats
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);

        // Add console logger for immediate feedback
        AddLogger(ConsoleLogger.Default);

        // Set validation options
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}