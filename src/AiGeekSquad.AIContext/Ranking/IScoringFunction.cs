using System;
using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking
{
    /// <summary>
    /// Represents a function that scores items of type T.
    /// </summary>
    /// <typeparam name="T">The type of items to score.</typeparam>
    public interface IScoringFunction<T>
    {
        /// <summary>
        /// Gets the name of this scoring function for debugging/logging.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Computes a score for the given item.
        /// </summary>
        /// <param name="item">The item to score.</param>
        /// <returns>A numeric score. Higher values indicate better matches.</returns>
        double ComputeScore(T item);
        
        /// <summary>
        /// Computes scores for multiple items in batch for efficiency.
        /// </summary>
        /// <param name="items">The items to score.</param>
        /// <returns>Array of scores corresponding to input items.</returns>
        double[] ComputeScores(IReadOnlyList<T> items);
    }
}