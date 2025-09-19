using System;
using System.Linq;

namespace AiGeekSquad.AIContext.Ranking.Normalizers;

/// <summary>
/// Percentile rank normalization.
/// </summary>
public class PercentileNormalizer : IScoreNormalizer
{
    /// <summary>
    /// Gets the name of this normalization strategy.
    /// </summary>
    public string Name => "Percentile";

    /// <summary>
    /// Normalizes an array of scores using percentile rank normalization.
    /// Each score is replaced by its percentile rank (0 to 1).
    /// </summary>
    /// <param name="scores">The scores to normalize.</param>
    /// <returns>Percentile rank normalized scores.</returns>
    public double[] Normalize(double[] scores)
    {
        if (scores == null)
            throw new ArgumentNullException(nameof(scores));

        if (scores.Length == 0)
            return scores;

        var sorted = scores.OrderBy(s => s).ToArray();
        var result = new double[scores.Length];

        for (int i = 0; i < scores.Length; i++)
        {
            // Find the rank of this score in the sorted array
            var rank = Array.BinarySearch(sorted, scores[i]);

            // Handle duplicate values by finding the first occurrence
            if (rank < 0)
            {
                rank = ~rank;
            }
            else
            {
                // Find the first occurrence of this value (with floating point tolerance)
                while (rank > 0 && Math.Abs(sorted[rank - 1] - scores[i]) < 1e-10)
                {
                    rank--;
                }
            }

            // Convert rank to percentile (0 to 1)
            result[i] = scores.Length == 1 ? 0.5 : (double)rank / (scores.Length - 1);
        }

        return result;
    }
}