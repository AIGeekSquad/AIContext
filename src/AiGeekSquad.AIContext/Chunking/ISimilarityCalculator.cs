using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Chunking;

/// <summary>
/// Provides functionality for calculating similarity between vectors.
/// </summary>
internal interface ISimilarityCalculator
{
    /// <summary>
    /// Calculates the cosine similarity between two vectors.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The cosine similarity value between 0 and 1, where 1 indicates identical vectors.</returns>
    double CalculateCosineSimilarity(Vector<double> vector1, Vector<double> vector2);

    /// <summary>
    /// Calculates the distance between two vectors based on their similarity.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>The distance value between 0 and 1, where 0 indicates identical vectors.</returns>
    double CalculateDistance(Vector<double> vector1, Vector<double> vector2);
}