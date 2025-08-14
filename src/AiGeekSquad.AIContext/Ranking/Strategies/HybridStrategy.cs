using System;
using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking.Strategies
{
    /// <summary>
    /// Hybrid strategy that combines weighted sum with Reciprocal Rank Fusion (RRF).
    /// </summary>
    public class HybridStrategy : IRankingStrategy
    {
        private readonly double _alpha; // Weight for weighted sum vs RRF
        private readonly WeightedSumStrategy _weightedSum;
        private readonly ReciprocalRankFusionStrategy _rrf;
        
        /// <summary>
        /// Gets the name of this strategy.
        /// </summary>
        public string Name => "Hybrid";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="HybridStrategy"/> class.
        /// </summary>
        /// <param name="alpha">The weight for weighted sum vs RRF (0.0 = pure RRF, 1.0 = pure weighted sum).</param>
        /// <param name="rrfK">The RRF constant parameter (typically 60).</param>
        public HybridStrategy(double alpha = 0.5, double rrfK = 60)
        {
            _alpha = Math.Max(0.0, Math.Min(1.0, alpha)); // Clamp to [0,1]
            _weightedSum = new WeightedSumStrategy();
            _rrf = new ReciprocalRankFusionStrategy(rrfK);
        }
        
        /// <summary>
        /// Combines multiple scores into a final ranking score using a hybrid of weighted sum and RRF.
        /// </summary>
        /// <param name="scores">The individual scores from each function.</param>
        /// <param name="weights">The weights for each score.</param>
        /// <param name="context">Additional context for ranking.</param>
        /// <returns>The combined hybrid score.</returns>
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
            
            var wsScore = _weightedSum.CombineScores(scores, weights, context);
            
            // RRF requires context, so if not provided, fall back to weighted sum only
            if (context == null)
            {
                return wsScore;
            }
            
            var rrfScore = _rrf.CombineScores(scores, weights, context);
            
            return _alpha * wsScore + (1 - _alpha) * rrfScore;
        }
    }
}