namespace AiGeekSquad.AIContext.Ranking
{
    /// <summary>
    /// Normalizes scores to a common scale.
    /// </summary>
    public interface IScoreNormalizer
    {
        /// <summary>
        /// Gets the name of this normalization strategy.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Normalizes an array of scores.
        /// </summary>
        /// <param name="scores">The scores to normalize.</param>
        /// <returns>Normalized scores.</returns>
        double[] Normalize(double[] scores);
    }
}