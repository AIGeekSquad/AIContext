using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

// Customer support knowledge base - login issue solutions
var solutions = new[]
{
    ("Reset password via email", Vector<double>.Build.DenseOfArray([0.9, 0.1, 0.0]), "auth"),
    ("Send password reset email", Vector<double>.Build.DenseOfArray([0.85, 0.15, 0.0]), "auth"), 
    ("Use forgot password feature", Vector<double>.Build.DenseOfArray([0.88, 0.12, 0.0]), "auth"),
    ("Contact support manually", Vector<double>.Build.DenseOfArray([0.3, 0.8, 0.4]), "support"),
    ("Clear browser cache", Vector<double>.Build.DenseOfArray([0.1, 0.2, 0.9]), "technical"),
    ("Try different browser", Vector<double>.Build.DenseOfArray([0.15, 0.25, 0.85]), "technical")
};

// Query: "user can't access account"
var query = Vector<double>.Build.DenseOfArray([0.9, 0.2, 0.1]);

Console.WriteLine("=== Query: 'User can't access account' ===\n");

// BEFORE: Traditional similarity search
Console.WriteLine("BEFORE - Traditional Search:");
var traditional = solutions
    .Select((sol, idx) => new { Index = idx, Solution = sol, Similarity = 1.0 - Distance.Cosine(query.ToArray(), sol.Item2.ToArray()) })
    .OrderByDescending(x => x.Similarity)
    .Take(3);

foreach (var result in traditional)
{
    Console.WriteLine($"• {result.Solution.Item1} ({result.Solution.Item3}) - {result.Similarity:F2}");
}

var traditionalCategories = traditional.Select(x => x.Solution.Item3).Distinct().Count();
Console.WriteLine($"❌ Problem: {traditionalCategories} categories - missing diverse approaches\n");

// AFTER: MMR search  
Console.WriteLine("AFTER - MMR Search:");
var mmrResults = MaximumMarginalRelevance.ComputeMMR(
    solutions.Select(s => s.Item2).ToList(),
    query,
    lambda: 0.7,
    topK: 3
);

foreach (var (index, _) in mmrResults)
{
    var sol = solutions[index];
    Console.WriteLine($"• {sol.Item1} ({sol.Item3}) - {1.0 - Distance.Cosine(query.ToArray(), sol.Item2.ToArray()):F2}");
}

var mmrCategories = mmrResults.Select(r => solutions[r.index].Item3).Distinct().Count();
Console.WriteLine($"✅ Improvement: {mmrCategories} categories - comprehensive solutions");
