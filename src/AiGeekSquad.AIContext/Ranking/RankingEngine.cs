using System;
using System.Collections.Generic;
using System.Linq;
using AiGeekSquad.AIContext.Ranking.Normalizers;
using AiGeekSquad.AIContext.Ranking.Strategies;

namespace AiGeekSquad.AIContext.Ranking
{
    /// <summary>
    /// Default implementation of the ranking engine.
    /// </summary>
    /// <typeparam name="T">The type of items to rank.</typeparam>
    public class RankingEngine<T> : IRankingEngine<T>
    {
        private readonly IScoreNormalizer _defaultNormalizer;
        private readonly IRankingStrategy _defaultStrategy;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RankingEngine{T}"/> class.
        /// </summary>
        /// <param name="defaultNormalizer">The default score normalizer to use. If null, uses MinMaxNormalizer.</param>
        /// <param name="defaultStrategy">The default ranking strategy to use. If null, uses WeightedSumStrategy.</param>
        public RankingEngine(
            IScoreNormalizer? defaultNormalizer = null,
            IRankingStrategy? defaultStrategy = null)
        {
            _defaultNormalizer = defaultNormalizer ?? new MinMaxNormalizer();
            _defaultStrategy = defaultStrategy ?? new WeightedSumStrategy();
        }
        
        /// <summary>
        /// Ranks items using the specified scoring functions and strategy.
        /// </summary>
        /// <param name="items">The items to rank.</param>
        /// <param name="scoringFunctions">The weighted scoring functions to apply.</param>
        /// <param name="strategy">Optional ranking strategy. If null, uses default.</param>
        /// <returns>Ranked results with scores.</returns>
        /// <exception cref="ArgumentException">Thrown when no scoring functions are provided.</exception>
        public IList<RankedResult<T>> Rank(
            IReadOnlyList<T> items,
            IReadOnlyList<WeightedScoringFunction<T>> scoringFunctions,
            IRankingStrategy? strategy = null)
        {
            if (items == null || items.Count == 0)
                return [];
            
            if (scoringFunctions == null || scoringFunctions.Count == 0)
                throw new ArgumentException("At least one scoring function is required", nameof(scoringFunctions));
            
            strategy = strategy ?? _defaultStrategy;
            
            // Compute all scores
            var scoreMatrix = new double[items.Count, scoringFunctions.Count];
            var normalizedScores = new double[items.Count, scoringFunctions.Count];
            
            for (int funcIdx = 0; funcIdx < scoringFunctions.Count; funcIdx++)
            {
                var func = scoringFunctions[funcIdx];
                var scores = func.Function.ComputeScores(items);
                
                // Normalize scores
                var normalizer = func.Normalizer ?? _defaultNormalizer;
                var normalized = normalizer.Normalize(scores);
                
                for (int itemIdx = 0; itemIdx < items.Count; itemIdx++)
                {
                    scoreMatrix[itemIdx, funcIdx] = scores[itemIdx];
                    normalizedScores[itemIdx, funcIdx] = normalized[itemIdx];
                }
            }
            
            // Combine scores and create results
            var results = new List<RankedResult<T>>(items.Count);
            var context = new RankingContext { TotalItems = items.Count };
            
            for (int i = 0; i < items.Count; i++)
            {
                context.CurrentIndex = i;
                
                // Extract scores for this item
                var itemScores = new double[scoringFunctions.Count];
                var weights = new double[scoringFunctions.Count];
                var scoreDict = new Dictionary<string, double>();
                
                for (int j = 0; j < scoringFunctions.Count; j++)
                {
                    itemScores[j] = normalizedScores[i, j];
                    weights[j] = scoringFunctions[j].Weight;
                    scoreDict[scoringFunctions[j].Function.Name] = scoreMatrix[i, j];
                }
                
                var finalScore = strategy.CombineScores(itemScores, weights, context);
                
                results.Add(new RankedResult<T>(
                    items[i],
                    finalScore,
                    scoreDict));
            }
            
            // Sort by final score (descending) and assign ranks
            results = results.OrderByDescending(r => r.FinalScore).ToList();
            for (int i = 0; i < results.Count; i++)
            {
                results[i].Rank = i + 1;
            }
            
            return results;
        }
        
        /// <summary>
        /// Ranks items and returns only the top K results.
        /// </summary>
        /// <param name="items">The items to rank.</param>
        /// <param name="scoringFunctions">The weighted scoring functions to apply.</param>
        /// <param name="k">The number of top results to return.</param>
        /// <param name="strategy">Optional ranking strategy. If null, uses default.</param>
        /// <returns>Top K ranked results with scores.</returns>
        public IList<RankedResult<T>> RankTopK(
            IReadOnlyList<T> items,
            IReadOnlyList<WeightedScoringFunction<T>> scoringFunctions,
            int k,
            IRankingStrategy? strategy = null)
        {
            if (k <= 0)
                return new List<RankedResult<T>>();
                
            var allResults = Rank(items, scoringFunctions, strategy);
            return allResults.Take(Math.Min(k, allResults.Count)).ToList();
        }
    }
}