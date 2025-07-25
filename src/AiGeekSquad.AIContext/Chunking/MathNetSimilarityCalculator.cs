using System;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Implements similarity calculations using Math.NET Numerics vector operations.
    /// </summary>
    internal class MathNetSimilarityCalculator : ISimilarityCalculator
    {
        /// <summary>
        /// Calculates the cosine similarity between two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cosine similarity value between 0 and 1, where 1 indicates identical vectors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either vector is null.</exception>
        /// <exception cref="ArgumentException">Thrown when vectors have different dimensions.</exception>
        public double CalculateCosineSimilarity(Vector<double> vector1, Vector<double> vector2)
        {
            if (vector1 == null)
                throw new ArgumentNullException(nameof(vector1));
            if (vector2 == null)
                throw new ArgumentNullException(nameof(vector2));
            if (vector1.Count != vector2.Count)
                throw new ArgumentException("Vectors must have the same dimension.");

            // Handle zero vectors to avoid division by zero
            var magnitude1 = vector1.L2Norm();
            var magnitude2 = vector2.L2Norm();

            if (magnitude1 == 0.0 || magnitude2 == 0.0)
                return 0.0;

            // Calculate cosine similarity: (v1 · v2) / (||v1|| * ||v2||)
            var dotProduct = vector1.DotProduct(vector2);
            var similarity = dotProduct / (magnitude1 * magnitude2);

            // Clamp to [0, 1] range to handle potential floating-point precision issues
            return Math.Max(0.0, Math.Min(1.0, similarity));
        }

        /// <summary>
        /// Calculates the distance between two vectors based on their similarity.
        /// Distance = 1 - CosineSimilarity
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The distance value between 0 and 1, where 0 indicates identical vectors.</returns>
        /// <exception cref="ArgumentNullException">Thrown when either vector is null.</exception>
        /// <exception cref="ArgumentException">Thrown when vectors have different dimensions.</exception>
        public double CalculateDistance(Vector<double> vector1, Vector<double> vector2)
        {
            var similarity = CalculateCosineSimilarity(vector1, vector2);
            return 1.0 - similarity;
        }
    }
}