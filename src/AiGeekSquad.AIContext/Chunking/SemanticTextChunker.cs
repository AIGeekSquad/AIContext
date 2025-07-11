using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides semantic text chunking functionality that splits text into semantically meaningful chunks
    /// based on embedding similarity analysis.
    /// </summary>
    public class SemanticTextChunker
    {
        private readonly ITokenCounter _tokenCounter;
        private readonly IEmbeddingGenerator _embeddingGenerator;
        private readonly ISimilarityCalculator _similarityCalculator;
        private readonly EmbeddingCache _embeddingCache;
        private readonly ITextSplitter _textSplitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticTextChunker"/> class.
        /// </summary>
        /// <param name="tokenCounter">The token counter for measuring text length.</param>
        /// <param name="embeddingGenerator">The embedding generator for creating vector representations.</param>
        /// <param name="similarityCalculator">The similarity calculator for comparing embeddings.</param>
        /// <param name="embeddingCache">The cache for storing computed embeddings.</param>
        /// <param name="textSplitter">The text splitter for dividing text into segments.</param>
        private SemanticTextChunker(
            ITokenCounter tokenCounter,
            IEmbeddingGenerator embeddingGenerator,
            ISimilarityCalculator similarityCalculator,
            EmbeddingCache embeddingCache,
            ITextSplitter textSplitter)
        {
            _tokenCounter = tokenCounter ?? throw new ArgumentNullException(nameof(tokenCounter));
            _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
            _similarityCalculator = similarityCalculator ?? throw new ArgumentNullException(nameof(similarityCalculator));
            _embeddingCache = embeddingCache ?? throw new ArgumentNullException(nameof(embeddingCache));
            _textSplitter = textSplitter ?? throw new ArgumentNullException(nameof(textSplitter));
        }

        /// <summary>
        /// Creates a new instance of <see cref="SemanticTextChunker"/> with the specified dependencies.
        /// </summary>
        /// <param name="tokenCounter">The token counter for measuring text length.</param>
        /// <param name="embeddingGenerator">The embedding generator for creating vector representations.</param>
        /// <param name="textSplitter">Optional text splitter for dividing text into segments. If null, uses default sentence splitter.</param>
        /// <returns>A new instance of <see cref="SemanticTextChunker"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public static SemanticTextChunker Create(
            ITokenCounter tokenCounter,
            IEmbeddingGenerator embeddingGenerator,
            ITextSplitter? textSplitter = null)
        {
            if (tokenCounter == null)
                throw new ArgumentNullException(nameof(tokenCounter));
            if (embeddingGenerator == null)
                throw new ArgumentNullException(nameof(embeddingGenerator));

            var similarityCalculator = new MathNetSimilarityCalculator();
            var embeddingCache = new EmbeddingCache();
            var splitter = textSplitter ?? SentenceTextSplitter.Default;

            return new SemanticTextChunker(tokenCounter, embeddingGenerator, similarityCalculator, embeddingCache, splitter);
        }

        /// <summary>
        /// Asynchronously chunks the specified text into semantically meaningful segments.
        /// </summary>
        /// <param name="text">The text to chunk.</param>
        /// <param name="options">The chunking options. If null, default options are used.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of text chunks.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public async IAsyncEnumerable<TextChunk> ChunkAsync(
            string text,
            SemanticChunkingOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            await foreach (var chunk in ChunkDocumentAsync(text, null, options, cancellationToken))
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Asynchronously chunks a document with associated metadata.
        /// </summary>
        /// <param name="text">The text to chunk.</param>
        /// <param name="metadata">Optional metadata to associate with the chunks.</param>
        /// <param name="options">The chunking options. If null, default options are used.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of text chunks with metadata.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public async IAsyncEnumerable<TextChunk> ChunkDocumentAsync(
            string text,
            IDictionary<string, object>? metadata = null,
            SemanticChunkingOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            options ??= SemanticChunkingOptions.Default;

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            // Step 1: Split text into segments using the text splitter
            var segments = new List<(string text, int startIndex, int endIndex)>();
            await foreach (var segment in _textSplitter.SplitAsync(text, cancellationToken))
            {
                segments.Add((segment.Text, segment.StartIndex, segment.EndIndex));
            }

            if (segments.Count == 0)
                yield break;

            // Step 2: Create segment groups with buffer context
            var sentenceGroups = CreateSegmentGroups(segments, options.BufferSize);

            // Step 3: Generate embeddings for sentence groups
            await GenerateEmbeddingsForGroups(sentenceGroups, options, cancellationToken);

            // Step 4: Calculate similarities and distances
            var distances = CalculateDistancesBetweenGroups(sentenceGroups);

            // Step 5: Identify breakpoints using percentile threshold
            var breakpoints = IdentifyBreakpoints(distances, options.BreakpointPercentileThreshold);

            // Step 6: Create chunks based on breakpoints
            await foreach (var chunk in CreateChunksFromBreakpoints(segments, breakpoints, metadata, options, cancellationToken))
            {
                yield return chunk;
            }
        }


        /// <summary>
        /// Creates segment groups with buffer context around each segment.
        /// </summary>
        /// <param name="segments">The list of text segments with their positions.</param>
        /// <param name="bufferSize">The number of segments before and after to include as context.</param>
        /// <returns>A list of sentence groups.</returns>
        private static List<SentenceGroup> CreateSegmentGroups(
            List<(string text, int startIndex, int endIndex)> segments,
            int bufferSize)
        {
            var groups = new List<SentenceGroup>();

            for (var i = 0; i < segments.Count; i++)
            {
                var groupSegments = new List<string>();
                var groupStartIndex = segments[i].startIndex;
                var groupEndIndex = segments[i].endIndex;

                // Add segments with buffer context
                for (var j = Math.Max(0, i - bufferSize); j <= Math.Min(segments.Count - 1, i + bufferSize); j++)
                {
                    groupSegments.Add(segments[j].text);
                    if (j == Math.Max(0, i - bufferSize))
                        groupStartIndex = segments[j].startIndex;
                    if (j == Math.Min(segments.Count - 1, i + bufferSize))
                        groupEndIndex = segments[j].endIndex;
                }

                groups.Add(new SentenceGroup(groupSegments, groupStartIndex, groupEndIndex));
            }

            return groups;
        }

        /// <summary>
        /// Generates embeddings for all sentence groups, using caching when enabled.
        /// </summary>
        /// <param name="sentenceGroups">The sentence groups to generate embeddings for.</param>
        /// <param name="options">The chunking options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private async Task GenerateEmbeddingsForGroups(
            List<SentenceGroup> sentenceGroups,
            SemanticChunkingOptions options,
            CancellationToken cancellationToken)
        {
            var textsToEmbed = new List<(SentenceGroup group, string text)>();

            // Check cache first if enabled
            foreach (var group in sentenceGroups)
            {
                if (options.EnableEmbeddingCaching &&
                    _embeddingCache.TryGetEmbedding(group.CombinedText, out var cachedEmbedding))
                {
                    group.Embedding = cachedEmbedding;
                }
                else
                {
                    textsToEmbed.Add((group, group.CombinedText));
                }
            }

            // Generate embeddings for uncached texts
            if (textsToEmbed.Count > 0)
            {
                var texts = textsToEmbed.Select(x => x.text);
                var embeddingsList = new List<MathNet.Numerics.LinearAlgebra.Vector<double>>();

                await foreach (var embedding in _embeddingGenerator.GenerateBatchEmbeddingsAsync(texts, cancellationToken))
                {
                    embeddingsList.Add(embedding);
                }

                for (var i = 0; i < textsToEmbed.Count && i < embeddingsList.Count; i++)
                {
                    var group = textsToEmbed[i].group;
                    var embedding = embeddingsList[i];

                    group.Embedding = embedding;

                    // Cache the embedding if enabled
                    if (options.EnableEmbeddingCaching)
                    {
                        _embeddingCache.StoreEmbedding(group.CombinedText, embedding);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates distances between adjacent sentence groups.
        /// </summary>
        /// <param name="sentenceGroups">The sentence groups with embeddings.</param>
        /// <returns>A list of distances between adjacent groups.</returns>
        private List<double> CalculateDistancesBetweenGroups(List<SentenceGroup> sentenceGroups)
        {
            var distances = new List<double>();

            for (var i = 0; i < sentenceGroups.Count - 1; i++)
            {
                var group1 = sentenceGroups[i];
                var group2 = sentenceGroups[i + 1];

                if (group1.HasEmbedding && group2.HasEmbedding)
                {
                    var distance = _similarityCalculator.CalculateDistance(group1.Embedding!, group2.Embedding!);
                    distances.Add(distance);
                }
                else
                {
                    // Fallback: assume medium distance if embeddings are missing
                    distances.Add(0.5);
                }
            }

            return distances;
        }

        /// <summary>
        /// Identifies breakpoints based on distance percentile threshold.
        /// </summary>
        /// <param name="distances">The distances between sentence groups.</param>
        /// <param name="percentileThreshold">The percentile threshold for identifying breakpoints.</param>
        /// <returns>A list of breakpoint indices.</returns>
        private static List<int> IdentifyBreakpoints(List<double> distances, double percentileThreshold)
        {
            if (distances.Count == 0)
                return new List<int>();

            var threshold = VectorExtensions.CalculatePercentile(distances, percentileThreshold);
            return VectorExtensions.FindBreakpoints(distances, threshold).ToList();
        }

        /// <summary>
        /// Creates text chunks based on identified breakpoints.
        /// </summary>
        /// <param name="segments">The list of text segments with their positions.</param>
        /// <param name="breakpoints">The identified breakpoint indices.</param>
        /// <param name="metadata">Optional metadata to associate with chunks.</param>
        /// <param name="options">The chunking options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of text chunks.</returns>
        private async IAsyncEnumerable<TextChunk> CreateChunksFromBreakpoints(List<(string text, int startIndex, int endIndex)> segments,
            List<int> breakpoints,
            IDictionary<string, object>? metadata,
            SemanticChunkingOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var chunkStartIndex = 0;
            var validChunks = new List<TextChunk>();

            // Process breakpoints to create chunks
            foreach (var breakpoint in breakpoints.Concat(new[] { segments.Count - 1 }))
            {
                if (breakpoint >= chunkStartIndex)
                {
                    var chunkSegments = segments.Skip(chunkStartIndex).Take(breakpoint - chunkStartIndex + 1).ToList();

                    if (chunkSegments.Count > 0)
                    {
                        var chunkText = string.Join(" ", chunkSegments.Select(s => s.text));
                        var tokenCount = await _tokenCounter.CountTokensAsync(chunkText, cancellationToken);

                        // Check if chunk meets token requirements
                        if (tokenCount >= options.MinTokensPerChunk && tokenCount <= options.MaxTokensPerChunk)
                        {
                            var startIndex = chunkSegments.First().startIndex;
                            var endIndex = chunkSegments.Last().endIndex;

                            var chunkMetadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>();
                            chunkMetadata["TokenCount"] = tokenCount;
                            chunkMetadata["SegmentCount"] = chunkSegments.Count;

                            var chunk = new TextChunk(chunkText, startIndex, endIndex, chunkMetadata);
                            validChunks.Add(chunk);
                        }
                    }

                    chunkStartIndex = breakpoint + 1;
                }
            }

            // If no valid chunks were created, create fallback chunks
            if (validChunks.Count == 0 && segments.Count > 0)
            {
                // Create a single chunk with all segments, ignoring minimum token requirements as fallback
                var allText = string.Join(" ", segments.Select(s => s.text));
                var totalTokenCount = await _tokenCounter.CountTokensAsync(allText, cancellationToken);

                if (totalTokenCount <= options.MaxTokensPerChunk)
                {
                    // Single chunk with all content
                    var chunkMetadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>();
                    chunkMetadata["TokenCount"] = totalTokenCount;
                    chunkMetadata["SegmentCount"] = segments.Count;
                    chunkMetadata["IsFallback"] = true;

                    validChunks.Add(new TextChunk(allText, segments.First().startIndex, segments.Last().endIndex, chunkMetadata));
                }
                else
                {
                    // Create sentence-by-sentence chunks as last resort
                    foreach (var (text, startIndex, endIndex) in segments)
                    {
                        var tokenCount = await _tokenCounter.CountTokensAsync(text, cancellationToken);
                        if (tokenCount <= options.MaxTokensPerChunk)
                        {
                            var chunkMetadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>();
                            chunkMetadata["TokenCount"] = tokenCount;
                            chunkMetadata["SegmentCount"] = 1;
                            chunkMetadata["IsFallback"] = true;

                            validChunks.Add(new TextChunk(text, startIndex, endIndex, chunkMetadata));
                        }
                    }
                }
            }

            // Return all valid chunks
            foreach (var chunk in validChunks)
            {
                yield return chunk;
            }
        }
    }
}