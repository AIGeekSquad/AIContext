using System;
using System.Linq;

namespace AiGeekSquad.AIContext.Ranking.Normalizers;

/// <summary>
/// Min-Max normalization to [0, 1] range.
/// </summary>
public class MinMaxNormalizer : IScoreNormalizer
{
    /// <summary>
    /// Gets the name of this normalization strategy.
    /// </summary>
    public string Name => "MinMax";

    /// <summary>
    /// Normalizes an array of scores to the [0, 1] range using min-max normalization.
    /// </summary>
    /// <param name="scores">The scores to normalize.</param>
    /// <returns>Normalized scores in the [0, 1] range.</returns>
    public double[] Normalize(double[] scores)
    {
        if (scores == null)
            throw new ArgumentNullException(nameof(scores));

        if (scores.Length == 0)
            return scores;

        var min = scores.Min();
        var max = scores.Max();
        var range = max - min;

        // If all scores are the same (within floating point precision), return 0.5 for all
        if (Math.Abs(range) < 1e-10)
            return scores.Select(_ => 0.5).ToArray();

        return scores.Select(s => (s - min) / range).ToArray();
    }
}