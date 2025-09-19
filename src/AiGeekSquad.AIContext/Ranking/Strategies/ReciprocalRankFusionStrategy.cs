using System;
using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking.Strategies;

/// <summary>
/// Reciprocal Rank Fusion (RRF) strategy for combining multiple ranking scores.
/// </summary>
public class ReciprocalRankFusionStrategy : IRankingStrategy
{
    private readonly double _k;

    /// <summary>
    /// Gets the name of this strategy.
    /// </summary>
    public string Name => "RRF";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReciprocalRankFusionStrategy"/> class.
    /// </summary>
    /// <param name="k">The RRF constant parameter (typically 60). Higher values reduce the impact of top-ranked items.</param>
    public ReciprocalRankFusionStrategy(double k = 60)
    {
        _k = k;
    }

    /// <summary>
    /// Combines multiple scores into a final ranking score using Reciprocal Rank Fusion.
    /// </summary>
    /// <param name="scores">The individual scores from each function.</param>
    /// <param name="weights">The weights for each score.</param>
    /// <param name="context">Additional context for ranking, including total items count.</param>
    /// <returns>The combined RRF score.</returns>
    /// <exception cref="ArgumentException">Thrown when scores and weights have different counts.</exception>
    /// <exception cref="ArgumentNullException">Thrown when scores, weights, or context are null.</exception>
    public double CombineScores(
        IReadOnlyList<double> scores,
        IReadOnlyList<double> weights,
        RankingContext? context = null)
    {
        if (scores == null)
            throw new ArgumentNullException(nameof(scores));
        if (weights == null)
            throw new ArgumentNullException(nameof(weights));
        if (context == null)
            throw new ArgumentNullException(nameof(context), "RRF strategy requires context with TotalItems");
        if (scores.Count != weights.Count)
            throw new ArgumentException("Scores and weights must have same count");

        double rrfScore = 0;
        for (int i = 0; i < scores.Count; i++)
        {
            // Convert normalized score to pseudo-rank (higher score = lower rank number)
            // Assuming scores are normalized to [0,1], we convert to rank position
            var rank = Math.Max(1, context.TotalItems - (int)(scores[i] * (context.TotalItems - 1)));

            // Apply RRF formula: weight / (k + rank)
            rrfScore += weights[i] / (_k + rank);
        }

        return rrfScore;
    }
}