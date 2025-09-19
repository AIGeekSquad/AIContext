using System;
using System.Linq;

namespace AiGeekSquad.AIContext.Ranking.Normalizers;

/// <summary>
/// Z-score normalization (standardization).
/// </summary>
public class ZScoreNormalizer : IScoreNormalizer
{
    /// <summary>
    /// Gets the name of this normalization strategy.
    /// </summary>
    public string Name => "ZScore";

    /// <summary>
    /// Normalizes an array of scores using Z-score normalization (standardization).
    /// Transforms scores to have mean = 0 and standard deviation = 1.
    /// </summary>
    /// <param name="scores">The scores to normalize.</param>
    /// <returns>Z-score normalized scores.</returns>
    public double[] Normalize(double[] scores)
    {
        if (scores == null)
            throw new ArgumentNullException(nameof(scores));

        if (scores.Length == 0)
            return scores;

        var mean = scores.Average();
        var variance = scores.Select(s => Math.Pow(s - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        // If standard deviation is effectively 0 (within floating point precision), all scores are the same
        if (Math.Abs(stdDev) < 1e-10)
            return scores.Select(_ => 0.0).ToArray();

        return scores.Select(s => (s - mean) / stdDev).ToArray();
    }
}