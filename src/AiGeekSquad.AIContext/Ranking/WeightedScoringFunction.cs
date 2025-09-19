using System;

namespace AiGeekSquad.AIContext.Ranking;

/// <summary>
/// Represents a scoring function with an associated weight.
/// </summary>
/// <typeparam name="T">The type of items to score.</typeparam>
public class WeightedScoringFunction<T>
{
    /// <summary>
    /// The scoring function.
    /// </summary>
    public IScoringFunction<T> Function { get; }

    /// <summary>
    /// The weight to apply to this function's scores.
    /// Positive weights increase ranking, negative weights decrease ranking.
    /// </summary>
    public double Weight { get; }

    /// <summary>
    /// Optional normalization strategy for this function's scores.
    /// If null, uses the engine's default normalizer.
    /// </summary>
    public IScoreNormalizer? Normalizer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WeightedScoringFunction{T}"/> class.
    /// </summary>
    /// <param name="function">The scoring function.</param>
    /// <param name="weight">The weight to apply to this function's scores.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
    public WeightedScoringFunction(IScoringFunction<T> function, double weight)
    {
        Function = function ?? throw new ArgumentNullException(nameof(function));
        Weight = weight;
    }
}