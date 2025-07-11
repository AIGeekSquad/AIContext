using AiGeekSquad.AIContext.Chunking;
using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;

namespace AiGeekSquad.AIContext.Examples;

/// <summary>
/// Basic example of semantic text chunking with a simple embedding generator.
/// In production, replace SimpleEmbeddingGenerator with your actual embedding provider.
/// </summary>
public class BasicChunkingExample
{
    public static async Task RunAsync()
    {
        // Setup components
        var tokenCounter = new MLTokenCounter();
        var embeddingGenerator = new SimpleEmbeddingGenerator();
        
        // Create chunker
        var chunker = SemanticTextChunker.Create(tokenCounter, embeddingGenerator);
        
        // Configure options for demo
        var options = new SemanticChunkingOptions
        {
            MaxTokensPerChunk = 100,  // Small chunks for demo
            MinTokensPerChunk = 10,
            BreakpointPercentileThreshold = 0.75
        };
        
        // Sample text about AI and technology
        var text = @"
        Artificial intelligence is transforming how we work and live. Machine learning algorithms can now process 
        vast amounts of data to find patterns that humans might miss. Deep learning, a subset of machine learning, 
        uses neural networks to solve complex problems.
        
        In the business world, companies are adopting AI for customer service, fraud detection, and process automation. 
        Chatbots can handle routine customer inquiries, while sophisticated algorithms detect suspicious transactions 
        in real-time.
        
        The future of AI looks promising with advances in natural language processing and computer vision. However, 
        we must also consider the ethical implications of these technologies. Privacy, bias, and job displacement 
        are important concerns that need addressing.
        ";
        
        // Add metadata
        var metadata = new Dictionary<string, object>
        {
            ["Source"] = "AI Technology Overview",
            ["Category"] = "Technology",
            ["Author"] = "Example"
        };
        
        Console.WriteLine("=== Semantic Chunking Example ===\n");
        
        // Process chunks
        var chunkNumber = 1;
        await foreach (var chunk in chunker.ChunkDocumentAsync(text, metadata, options))
        {
            Console.WriteLine($"Chunk {chunkNumber}:");
            Console.WriteLine($"  Text: {chunk.Text.Trim()}");
            Console.WriteLine($"  Tokens: {chunk.Metadata["TokenCount"]}");
            Console.WriteLine($"  Segments: {chunk.Metadata["SegmentCount"]}");
            Console.WriteLine($"  Position: {chunk.StartIndex}-{chunk.EndIndex}");
            Console.WriteLine($"  Source: {chunk.Metadata["Source"]}");
            
            if (chunk.Metadata.ContainsKey("IsFallback"))
                Console.WriteLine("  [Used fallback logic]");
                
            Console.WriteLine();
            chunkNumber++;
        }
    }
}

/// <summary>
/// Simple embedding generator for demonstration purposes.
/// In production, replace this with OpenAI, Azure Cognitive Services, or your preferred provider.
/// </summary>
public class SimpleEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly Random _random = new(42); // Fixed seed for reproducible results
    private const int EmbeddingDimensions = 384; // Common dimension size
    
    public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
        IEnumerable<string> texts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var text in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Simulate API call delay
            await Task.Delay(10, cancellationToken);
            
            // Generate deterministic embedding based on text content
            var embedding = GenerateEmbedding(text);
            yield return embedding;
        }
    }
    
    private Vector<double> GenerateEmbedding(string text)
    {
        // Create a simple embedding based on text characteristics
        var values = new double[EmbeddingDimensions];
        var hash = text.GetHashCode();
        var localRandom = new Random(Math.Abs(hash));
        
        // Base random vector
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            values[i] = localRandom.NextGaussian() * 0.1;
        }
        
        // Add semantic meaning based on keywords
        var keywords = new Dictionary<string, int>
        {
            ["artificial", "intelligence", "ai", "machine", "learning"] = 0,
            ["business", "company", "customer", "service"] = 100,
            ["future", "technology", "computer", "vision"] = 200,
            ["ethical", "privacy", "bias", "concerns"] = 300
        };
        
        var textLower = text.ToLowerInvariant();
        foreach (var (keywordGroup, startIndex) in keywords)
        {
            var keywordList = keywordGroup.Split(", ");
            var strength = keywordList.Count(keyword => textLower.Contains(keyword)) * 0.3;
            
            for (int i = 0; i < 50 && startIndex + i < EmbeddingDimensions; i++)
            {
                values[startIndex + i] += strength;
            }
        }
        
        // Normalize to unit vector
        var magnitude = Math.Sqrt(values.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] /= magnitude;
            }
        }
        
        return Vector<double>.Build.DenseOfArray(values);
    }
}

// Extension method for Gaussian random numbers
public static class RandomExtensions
{
    public static double NextGaussian(this Random random)
    {
        // Box-Muller transform
        static double Generate(Random r)
        {
            double u1 = 1.0 - r.NextDouble();
            double u2 = 1.0 - r.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
        
        return Generate(random);
    }
}