using System;
using System.Linq;
using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics.LinearAlgebra;

// Customer support ticket routing - select diverse solution approaches
public class SupportTicketRouter
{
    public static void Main()
    {
        // Available solutions for "app crashes on startup"
        var solutions = new[]
        {
            ("Clear app cache and data", Vector<double>.Build.DenseOfArray([0.9, 0.1, 0.0])),
            ("Restart the application", Vector<double>.Build.DenseOfArray([0.85, 0.15, 0.0])),
            ("Reinstall the app", Vector<double>.Build.DenseOfArray([0.88, 0.12, 0.0])),
            ("Check system requirements", Vector<double>.Build.DenseOfArray([0.3, 0.8, 0.1])),
            ("Update device drivers", Vector<double>.Build.DenseOfArray([0.2, 0.1, 0.9])),
            ("Contact technical support", Vector<double>.Build.DenseOfArray([0.1, 0.9, 0.1]))
        };

        // User query: "app won't start"
        var query = Vector<double>.Build.DenseOfArray([0.9, 0.2, 0.1]);

        Console.WriteLine("=== Support Ticket: 'App won't start' ===\n");

        // Apply MMR to get diverse solutions
        var mmrResults = MaximumMarginalRelevance.ComputeMMR(
            vectors: solutions.Select(s => s.Item2).ToList(),
            query: query,
            lambda: 0.7,  // Balance relevance with diversity
            topK: 3
        );

        Console.WriteLine("Recommended solutions:");
        foreach (var (index, score) in mmrResults)
        {
            Console.WriteLine($"• {solutions[index].Item1}");
        }
    }
}