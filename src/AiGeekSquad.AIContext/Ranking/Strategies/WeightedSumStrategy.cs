using System;
using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking.Strategies;

/// <summary>
/// Simple weighted sum combination strategy.
/// </summary>
public class WeightedSumStrategy : IRankingStrategy
{
    /// <summary>
    /// Gets the name of this strategy.
    /// </summary>
    public string Name => "WeightedSum";

    /// <summary>
    /// Combines multiple scores into a final ranking score using weighted sum.
    /// </summary>
    /// <param name="scores">The individual scores from each function.</param>
    /// <param name="weights">The weights for each score.</param>
    /// <param name="context">Additional context for ranking (not used in this strategy).</param>
    /// <returns>The combined score as a weighted sum.</returns>
    /// <exception cref="ArgumentException">Thrown when scores and weights have different counts.</exception>
    /// <exception cref="ArgumentNullException">Thrown when scores or weights are null.</exception>
    public double CombineScores(
        IReadOnlyList<double> scores,
        IReadOnlyList<double> weights,
        RankingContext? context = null)
    {
        if (scores == null)
            throw new ArgumentNullException(nameof(scores));
        if (weights == null)
            throw new ArgumentNullException(nameof(weights));
        if (scores.Count != weights.Count)
            throw new ArgumentException("Scores and weights must have same count");

        double sum = 0;
        for (int i = 0; i < scores.Count; i++)
        {
            sum += scores[i] * weights[i];
        }

        return sum;
    }
}