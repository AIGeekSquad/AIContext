using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Examples;

/// <summary>
/// Example demonstrating Maximum Marginal Relevance (MMR) for diverse document selection.
/// </summary>
public class MMRExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== Maximum Marginal Relevance (MMR) Example ===\n");
        
        // Simulate document embeddings (in practice, these come from neural networks)
        var documents = CreateSampleDocuments();
        var documentTitles = new[]
        {
            "Introduction to Machine Learning",
            "Advanced Machine Learning Techniques", // Similar to #1
            "Basketball Training Guide",
            "Football Strategy Manual",             // Similar to #3
            "Cooking Italian Pasta",
            "French Cuisine Fundamentals",          // Similar to #5
            "Climate Change Research",
            "Environmental Conservation",           // Similar to #7
            "Stock Market Analysis",
            "Investment Portfolio Management"       // Similar to #9
        };
        
        // User query: interested in machine learning
        var query = Vector<double>.Build.DenseOfArray(new[] 
        { 
            0.9, 0.1, 0.0, 0.0, 0.0  // Strong signal for ML/tech content
        });
        
        Console.WriteLine("User Query: Interested in machine learning content");
        Console.WriteLine("Available Documents:");
        for (int i = 0; i < documentTitles.Length; i++)
        {
            Console.WriteLine($"  {i}: {documentTitles[i]}");
        }
        Console.WriteLine();
        
        // Demonstrate different lambda values
        DemonstrateMMR(documents, documentTitles, query, lambda: 1.0, "Pure Relevance");
        DemonstrateMMR(documents, documentTitles, query, lambda: 0.7, "Relevance-Focused");
        DemonstrateMMR(documents, documentTitles, query, lambda: 0.5, "Balanced");
        DemonstrateMMR(documents, documentTitles, query, lambda: 0.3, "Diversity-Focused");
        DemonstrateMMR(documents, documentTitles, query, lambda: 0.0, "Pure Diversity");
    }
    
    private static void DemonstrateMMR(
        List<Vector<double>> documents,
        string[] titles,
        Vector<double> query,
        double lambda,
        string description)
    {
        MMRExampleUtilities.ExecuteAndDisplayMMR(documents, titles, query, lambda, description);
    }
    
    private static List<Vector<double>> CreateSampleDocuments()
    {
        // Create document embeddings with deliberate similarity patterns
        return new List<Vector<double>>
        {
            // Machine Learning cluster
            Vector<double>.Build.DenseOfArray(new[] { 0.8, 0.2, 0.0, 0.0, 0.0 }), // ML intro
            Vector<double>.Build.DenseOfArray(new[] { 0.85, 0.15, 0.0, 0.0, 0.0 }), // Advanced ML (similar)
            
            // Sports cluster  
            Vector<double>.Build.DenseOfArray(new[] { 0.1, 0.8, 0.1, 0.0, 0.0 }), // Basketball
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.9, 0.1, 0.0, 0.0 }), // Football (similar)
            
            // Cooking cluster
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.1, 0.8, 0.1, 0.0 }), // Italian cooking
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0, 0.9, 0.1, 0.0 }), // French cooking (similar)
            
            // Environment cluster
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0, 0.1, 0.8, 0.1 }), // Climate change
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0, 0.0, 0.9, 0.1 }), // Conservation (similar)
            
            // Finance cluster
            Vector<double>.Build.DenseOfArray(new[] { 0.1, 0.0, 0.0, 0.1, 0.8 }), // Stock market
            Vector<double>.Build.DenseOfArray(new[] { 0.0, 0.0, 0.0, 0.1, 0.9 })  // Investment (similar)
        };
    }
    
}

/// <summary>
/// Example showing MMR in a practical RAG system context.
/// </summary>
public class RAGSystemExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== RAG System Context Selection Example ===\n");
        
        // Simulate retrieved context chunks from a vector database
        var contextCandidates = new List<(string text, Vector<double> embedding)>
        {
            ("Machine learning is a subset of artificial intelligence.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.9, 0.1, 0.0 })),
            ("Deep learning uses neural networks with multiple layers.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.85, 0.15, 0.0 })),
            ("Artificial intelligence can solve complex problems.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.8, 0.2, 0.0 })),
            ("Natural language processing helps computers understand text.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.7, 0.1, 0.2 })),
            ("Computer vision enables machines to interpret images.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.6, 0.1, 0.3 })),
            ("Robotics combines AI with mechanical engineering.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.4, 0.2, 0.4 })),
            ("Data science involves extracting insights from data.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.3, 0.4, 0.3 })),
            ("Statistics provides foundation for machine learning.", 
             Vector<double>.Build.DenseOfArray(new[] { 0.5, 0.3, 0.2 }))
        };
        
        // User question about AI applications
        var queryEmbedding = Vector<double>.Build.DenseOfArray(new[] { 0.8, 0.1, 0.1 });
        
        Console.WriteLine("User Question: 'What are the applications of artificial intelligence?'");
        Console.WriteLine("\nAvailable Context Chunks:");
        for (int i = 0; i < contextCandidates.Count; i++)
        {
            Console.WriteLine($"  {i}: {contextCandidates[i].text}");
        }
        
        // Select diverse context using MMR
        var selectedIndices = MaximumMarginalRelevance.ComputeMMR(
            vectors: contextCandidates.Select(c => c.embedding).ToList(),
            query: queryEmbedding,
            lambda: 0.8,  // Prioritize relevance but include some diversity
            topK: 4       // Limit context for LLM
        );
        
        MMRExampleUtilities.DisplayContextResults(selectedIndices, contextCandidates, queryEmbedding, "Selected Context (using MMR with λ = 0.8)");
        
        // Show what pure relevance would select
        var pureRelevance = MaximumMarginalRelevance.ComputeMMR(
            vectors: contextCandidates.Select(c => c.embedding).ToList(),
            query: queryEmbedding,
            lambda: 1.0,
            topK: 4
        );
        
        MMRExampleUtilities.DisplayContextResults(pureRelevance, contextCandidates, queryEmbedding, "For comparison - Pure Relevance Selection (λ = 1.0)");
        
        Console.WriteLine("\nNotice how MMR (λ = 0.8) provides more diverse context while maintaining relevance!");
    }
    
}