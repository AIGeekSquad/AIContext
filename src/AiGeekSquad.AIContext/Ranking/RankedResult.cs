using System;
using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking
{
    /// <summary>
    /// Represents a ranked item with its scores.
    /// </summary>
    /// <typeparam name="T">The type of the ranked item.</typeparam>
    public class RankedResult<T>
    {
        /// <summary>
        /// The original item.
        /// </summary>
        public T Item { get; }
        
        /// <summary>
        /// The final combined score used for ranking.
        /// </summary>
        public double FinalScore { get; }
        
        /// <summary>
        /// Individual scores from each scoring function.
        /// Key is the function name, value is the normalized score.
        /// </summary>
        public IReadOnlyDictionary<string, double> IndividualScores { get; }
        
        /// <summary>
        /// The rank position (1-based).
        /// </summary>
        public int Rank { get; set; }
        
        /// <summary>
        /// Additional metadata about the ranking.
        /// </summary>
        public Dictionary<string, object> Metadata { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RankedResult{T}"/> class.
        /// </summary>
        /// <param name="item">The original item.</param>
        /// <param name="finalScore">The final combined score used for ranking.</param>
        /// <param name="individualScores">Individual scores from each scoring function.</param>
        public RankedResult(
            T item,
            double finalScore,
            Dictionary<string, double> individualScores)
        {
            Item = item;
            FinalScore = finalScore;
            IndividualScores = individualScores ?? throw new ArgumentNullException(nameof(individualScores));
            Metadata = new Dictionary<string, object>();
        }
    }
}