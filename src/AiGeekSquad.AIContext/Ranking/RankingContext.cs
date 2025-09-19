using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Ranking;

/// <summary>
/// Provides context for ranking operations.
/// </summary>
public class RankingContext
{
    /// <summary>
    /// Total number of items being ranked.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Current item index being processed.
    /// </summary>
    public int CurrentIndex { get; set; }

    /// <summary>
    /// Additional parameters for the ranking strategy.
    /// </summary>
    public Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RankingContext"/> class.
    /// </summary>
    public RankingContext()
    {
        Parameters = new Dictionary<string, object>();
    }
}