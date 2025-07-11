namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides configuration options for semantic text chunking.
    /// </summary>
    public class SemanticChunkingOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of tokens per chunk.
        /// Default is 512 tokens.
        /// </summary>
        public int MaxTokensPerChunk { get; set; } = 512;

        /// <summary>
        /// Gets or sets the minimum number of tokens per chunk.
        /// Default is 10 tokens to allow for smaller chunks.
        /// </summary>
        public int MinTokensPerChunk { get; set; } = 10;

        /// <summary>
        /// Gets or sets the buffer size for sentence grouping.
        /// This determines how many sentences before and after the current sentence
        /// are included when generating embeddings for context.
        /// Default is 1 sentence.
        /// </summary>
        public int BufferSize { get; set; } = 1;

        /// <summary>
        /// Gets or sets the percentile threshold for identifying semantic breakpoints.
        /// This value (0.0 to 1.0) determines at what percentile of distance values
        /// a breakpoint is considered significant enough to create a chunk boundary.
        /// Default is 0.75 (75th percentile) for more reasonable chunk boundaries.
        /// </summary>
        public double BreakpointPercentileThreshold { get; set; } = 0.75;

        /// <summary>
        /// Gets or sets the minimum similarity threshold for merging adjacent chunks.
        /// Chunks with similarity above this threshold may be merged if they don't exceed token limits.
        /// Default is 0.8.
        /// </summary>
        public double MinSimilarityThreshold { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets a value indicating whether to enable embedding caching.
        /// When enabled, embeddings for identical text segments are cached to improve performance.
        /// Default is true.
        /// </summary>
        public bool EnableEmbeddingCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of cached embeddings to store.
        /// This helps prevent excessive memory usage when processing large documents.
        /// Default is 1000.
        /// </summary>
        public int MaxCacheSize { get; set; } = 1000;

        /// <summary>
        /// Creates a new instance of <see cref="SemanticChunkingOptions"/> with default values.
        /// </summary>
        /// <returns>A new <see cref="SemanticChunkingOptions"/> instance with default configuration.</returns>
        public static SemanticChunkingOptions Default => new SemanticChunkingOptions();
    }
}