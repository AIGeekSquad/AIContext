using AiGeekSquad.AIContext.Examples;

// AIContext Examples Console Application
// Usage: dotnet run [example-name]
// Examples: BasicChunking, MMR

if (args.Length == 0)
{
    Console.WriteLine("=== AIContext Examples ===");
    Console.WriteLine("Usage: dotnet run [example-name]");
    Console.WriteLine();
    Console.WriteLine("Available examples:");
    Console.WriteLine("  BasicChunking  - Semantic text chunking demonstration");
    Console.WriteLine("  MMR           - Maximum Marginal Relevance algorithm demonstration");
    Console.WriteLine();
    Console.WriteLine("Example usage:");
    Console.WriteLine("  dotnet run BasicChunking");
    Console.WriteLine("  dotnet run MMR");
    return;
}

var exampleName = args[0].ToLowerInvariant();

try
{
    switch (exampleName)
    {
        case "basicchunking":
        case "chunking":
            Console.WriteLine("Running Basic Chunking Example...\n");
            await BasicChunkingExample.RunAsync();
            break;
            
        case "mmr":
        case "maxmarginalrelevance":
            Console.WriteLine("Running MMR Examples...\n");
            MMRExample.RunExample();
            Console.WriteLine();
            RAGSystemExample.RunExample();
            break;
            
        default:
            Console.WriteLine($"Unknown example: {args[0]}");
            Console.WriteLine("Available examples: BasicChunking, MMR");
            Environment.Exit(1);
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error running example: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}