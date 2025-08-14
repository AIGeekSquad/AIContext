using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking
{
    /// <summary>
    /// Defines a strategy for combining scores from multiple functions.
    /// </summary>
    public interface IRankingStrategy
    {
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Combines multiple scores into a final ranking score.
        /// </summary>
        /// <param name="scores">The individual scores from each function.</param>
        /// <param name="weights">The weights for each score.</param>
        /// <param name="context">Additional context for ranking.</param>
        /// <returns>The combined score.</returns>
        double CombineScores(
            IReadOnlyList<double> scores,
            IReadOnlyList<double> weights,
            RankingContext? context = null);
    }
}