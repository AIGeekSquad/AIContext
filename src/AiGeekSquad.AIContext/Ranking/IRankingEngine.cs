using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking;

/// <summary>
/// Main interface for ranking items using multiple weighted scoring functions.
/// </summary>
/// <typeparam name="T">The type of items to rank.</typeparam>
public interface IRankingEngine<T>
{
    /// <summary>
    /// Ranks items using the specified scoring functions and strategy.
    /// </summary>
    /// <param name="items">The items to rank.</param>
    /// <param name="scoringFunctions">The weighted scoring functions to apply.</param>
    /// <param name="strategy">Optional ranking strategy. If null, uses default.</param>
    /// <returns>Ranked results with scores.</returns>
    IList<RankedResult<T>> Rank(
        IReadOnlyList<T> items,
        IReadOnlyList<WeightedScoringFunction<T>> scoringFunctions,
        IRankingStrategy? strategy = null);

    /// <summary>
    /// Ranks items and returns only the top K results.
    /// </summary>
    /// <param name="items">The items to rank.</param>
    /// <param name="scoringFunctions">The weighted scoring functions to apply.</param>
    /// <param name="k">The number of top results to return.</param>
    /// <param name="strategy">Optional ranking strategy. If null, uses default.</param>
    /// <returns>Top K ranked results with scores.</returns>
    IList<RankedResult<T>> RankTopK(
        IReadOnlyList<T> items,
        IReadOnlyList<WeightedScoringFunction<T>> scoringFunctions,
        int k,
        IRankingStrategy? strategy = null);
}