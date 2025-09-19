using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext.Ranking;

namespace AiGeekSquad.AIContext.Examples;

/// <summary>
/// Shared utilities for MMR examples to eliminate code duplication.
/// </summary>
public static class MMRExampleUtilities
{
    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    /// <param name="a">First vector</param>
    /// <param name="b">Second vector</param>
    /// <returns>Cosine similarity score between 0 and 1</returns>
    public static double CalculateCosineSimilarity(Vector<double> a, Vector<double> b)
    {
        var dotProduct = a.DotProduct(b);
        var magnitudeA = Math.Sqrt(a.DotProduct(a));
        var magnitudeB = Math.Sqrt(b.DotProduct(b));
        
        if (magnitudeA == 0 || magnitudeB == 0)
            return 0.0;
            
        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Displays MMR results with relevance scores for document selection.
    /// </summary>
    /// <param name="results">MMR computation results</param>
    /// <param name="documents">Document vectors</param>
    /// <param name="titles">Document titles</param>
    /// <param name="query">Query vector</param>
    /// <param name="description">Description of the MMR configuration</param>
    /// <param name="lambda">Lambda value used</param>
    public static void DisplayMMRResults(
        List<(int index, Vector<double> embedding)> results,
        List<Vector<double>> documents,
        string[] titles,
        Vector<double> query,
        string description,
        double lambda)
    {
        Console.WriteLine($"--- {description} (λ = {lambda}) ---");
        
        foreach (var (index, _) in results)
        {
            var relevanceScore = CalculateCosineSimilarity(query, documents[index]);
            Console.WriteLine($"  {index}: {titles[index]} (relevance: {relevanceScore:F3})");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Displays MMR results with relevance scores for context selection.
    /// </summary>
    /// <param name="results">MMR computation results</param>
    /// <param name="contextCandidates">Context candidates with text and embeddings</param>
    /// <param name="query">Query vector</param>
    /// <param name="title">Title for the results section</param>
    public static void DisplayContextResults(
        List<(int index, Vector<double> embedding)> results,
        List<(string text, Vector<double> embedding)> contextCandidates,
        Vector<double> query,
        string title)
    {
        Console.WriteLine($"\n{title}:");
        foreach (var (index, _) in results)
        {
            var relevance = CalculateCosineSimilarity(query, contextCandidates[index].embedding);
            Console.WriteLine($"  {index}: {contextCandidates[index].text} (relevance: {relevance:F3})");
        }
    }

    /// <summary>
    /// Executes MMR computation and displays results for document selection scenarios.
    /// </summary>
    /// <param name="documents">Document vectors</param>
    /// <param name="titles">Document titles</param>
    /// <param name="query">Query vector</param>
    /// <param name="lambda">Lambda parameter for MMR</param>
    /// <param name="description">Description of the MMR configuration</param>
    /// <param name="topK">Number of results to return</param>
    public static void ExecuteAndDisplayMMR(
        List<Vector<double>> documents,
        string[] titles,
        Vector<double> query,
        double lambda,
        string description,
        int topK = 5)
    {
        var results = MaximumMarginalRelevance.ComputeMMR(
            vectors: documents,
            query: query,
            lambda: lambda,
            topK: topK
        );
        
        DisplayMMRResults(results, documents, titles, query, description, lambda);
    }
}