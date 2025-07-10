using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AiGeekSquad.AIContext
{
    /// <summary>
    /// Implements the Maximum Marginal Relevance (MMR) algorithm for diverse document selection.
    /// 
    /// MMR balances relevance and diversity in document retrieval by selecting items that are 
    /// both relevant to the query and diverse from already selected items. The algorithm 
    /// iteratively selects the item that maximizes the weighted combination of:
    /// - Relevance: similarity to the query vector
    /// - Diversity: dissimilarity to already selected items
    /// </summary>
    public static class MaximumMarginalRelevance
    {
        /// <summary>
        /// Computes Maximum Marginal Relevance selection from a collection of vectors.
        /// </summary>
        /// <param name="vectors">The collection of vectors to select from</param>
        /// <param name="query">The query vector to compare against</param>
        /// <param name="lambda">
        /// Controls the trade-off between relevance and diversity (0.0 to 1.0).
        /// - 1.0 = pure relevance (ignores diversity)
        /// - 0.0 = pure diversity (ignores relevance)
        /// - 0.5 = balanced approach (default)
        /// </param>
        /// <param name="topK">
        /// Maximum number of items to select. If null, selects all items.
        /// If greater than the number of available vectors, returns all vectors.
        /// </param>
        /// <returns>
        /// A list of tuples containing the original index and vector for each selected item,
        /// ordered by selection priority (most relevant/diverse first).
        /// </returns>
        public static List<(int index, Vector<double> embedding)> ComputeMMR(
            List<Vector<double>> vectors, 
            Vector<double> query, 
            double lambda = 0.5, 
            int? topK = null)
        {
            if (vectors == null || vectors.Count == 0) return new List<(int, Vector<double>)>();
            
            int k = Math.Min(topK ?? vectors.Count, vectors.Count);
            if (k <= 0) return new List<(int, Vector<double>)>();
            if (k >= vectors.Count) return vectors.Select((v, i) => (i, v)).ToList();
            
            var queryArray = query.ToArray();
            var vectorArrays = vectors.Select(v => v.ToArray()).ToArray();
            
            // Pre-compute all query similarities once for efficiency
            var querySimilarities = new double[vectors.Count];
            for (int i = 0; i < vectors.Count; i++)
            {
                querySimilarities[i] = 1.0 - Distance.Cosine(vectorArrays[i], queryArray);
            }
            
            var selectedIndices = new List<int>(k);
            var remainingIndices = new bool[vectors.Count];
            Array.Fill(remainingIndices, true);
            
            // Iteratively select k items using MMR scoring
            for (int iteration = 0; iteration < k; iteration++)
            {
                int bestIndex = -1;
                double bestScore = double.MinValue;
                
                // Evaluate all remaining candidates
                for (int i = 0; i < vectors.Count; i++)
                {
                    if (!remainingIndices[i]) continue;
                    
                    // Relevance component: similarity to query
                    double relevanceScore = lambda * querySimilarities[i];
                    
                    // Diversity component: dissimilarity to already selected items
                    double diversityScore;
                    if (selectedIndices.Count == 0)
                    {
                        // First selection: only diversity weight matters
                        diversityScore = 1.0 - lambda;
                    }
                    else
                    {
                        // Compute average similarity to already selected items
                        double avgSimilarity = 0.0;
                        for (int j = 0; j < selectedIndices.Count; j++)
                        {
                            double similarity = 1.0 - Distance.Cosine(vectorArrays[i], vectorArrays[selectedIndices[j]]);
                            avgSimilarity += similarity;
                        }
                        avgSimilarity /= selectedIndices.Count;
                        
                        // Diversity score: higher when less similar to selected items
                        diversityScore = (1.0 - lambda) * (1.0 - avgSimilarity);
                    }
                    
                    double totalScore = relevanceScore + diversityScore;
                    
                    if (totalScore > bestScore)
                    {
                        bestScore = totalScore;
                        bestIndex = i;
                    }
                }
                
                // Break if no valid candidate found
                if (bestIndex == -1) break;
                
                // Select the best candidate
                selectedIndices.Add(bestIndex);
                remainingIndices[bestIndex] = false;
            }
            
            return selectedIndices.Select(i => (i, vectors[i])).ToList();
        }
    }
}