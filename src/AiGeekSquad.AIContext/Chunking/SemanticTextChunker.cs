using MathNet.Numerics.LinearAlgebra;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Chunking;

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
    /// Asynchronously chunks a document with associated metadata.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="metadata">Optional metadata to associate with the chunks.</param>
    /// <param name="options">The chunking options. If null, default options are used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of text chunks with metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public IAsyncEnumerable<TextChunk> ChunkAsync(
        string text,
        SemanticChunkingOptions? options = null,
        IDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return ChunkAsyncIterator(text, options, metadata, cancellationToken);
    }

    private async IAsyncEnumerable<TextChunk> ChunkAsyncIterator(
        string text,
        SemanticChunkingOptions? options,
        IDictionary<string, object>? metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        options ??= SemanticChunkingOptions.Default;

        if (string.IsNullOrWhiteSpace(text))
            yield break;

        await foreach (var chunk in ChunkAsyncCore(text, options, metadata, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Core implementation for asynchronously chunking text using semantic similarity analysis.
    /// </summary>
    private async IAsyncEnumerable<TextChunk> ChunkAsyncCore(
        string text,
        SemanticChunkingOptions options,
        IDictionary<string, object>? metadata,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Step 1: Split text into segments using the text splitter
        var segments = new List<(string text, int startIndex, int endIndex)>();
        await foreach (var segment in _textSplitter.SplitAsync(text, cancellationToken))
        {
            segments.Add((segment.Text, segment.StartIndex, segment.EndIndex));
        }

        if (segments.Count == 0)
            yield break;

        // Step 1.5: Validate and split segments that exceed token limits
        var validatedSegments = await ValidateAndSplitSegments(segments, options, cancellationToken);

        if (validatedSegments.Count == 0)
            yield break;

        // Step 2: Create segment groups with buffer context
        var sentenceGroups = CreateSegmentGroups(validatedSegments, options.BufferSize);

        // Step 2.5: Validate segment groups don't exceed token limits
        var validatedGroups = await ValidateSegmentGroups(sentenceGroups, options, cancellationToken);

        // Step 3: Generate embeddings for sentence groups
        await GenerateEmbeddingsForGroups(validatedGroups, options, cancellationToken);

        // Step 4: Calculate similarities and distances
        var distances = CalculateDistancesBetweenGroups(validatedGroups);

        // Step 5: Identify breakpoints using percentile threshold
        var breakpoints = IdentifyBreakpoints(distances, options.BreakpointPercentileThreshold);

        // Step 6: Create chunks based on breakpoints
        await foreach (var chunk in CreateChunksFromBreakpoints(validatedSegments, breakpoints, metadata, options, cancellationToken))
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
    /// Validates and splits segments that exceed token limits to ensure they can be processed by the embedding generator.
    /// </summary>
    /// <param name="segments">The list of text segments to validate.</param>
    /// <param name="options">The chunking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of validated segments that respect token limits.</returns>
    private async Task<List<(string text, int startIndex, int endIndex)>> ValidateAndSplitSegments(
        List<(string text, int startIndex, int endIndex)> segments,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var validatedSegments = new List<(string text, int startIndex, int endIndex)>();

        foreach (var segment in segments)
        {
            var tokenCount = await _tokenCounter.CountTokensAsync(segment.text, cancellationToken);

            if (tokenCount <= options.MaxTokensPerChunk)
            {
                // Segment is within limits
                validatedSegments.Add(segment);
            }
            else
            {
                // Segment exceeds token limit, split it further
                var splitSegments = await SplitOversizedSegment(segment, options, cancellationToken);
                validatedSegments.AddRange(splitSegments);
            }
        }

        return validatedSegments;
    }

    /// <summary>
    /// Splits an oversized segment into smaller segments that respect token limits.
    /// </summary>
    /// <param name="segment">The oversized segment to split.</param>
    /// <param name="options">The chunking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of smaller segments that respect token limits.</returns>
    private async Task<List<(string text, int startIndex, int endIndex)>> SplitOversizedSegment(
        (string text, int startIndex, int endIndex) segment,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var words = segment.text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length <= 1)
        {
            return HandleSingleWordSegment(segment);
        }

        return await ProcessWordsIntoSegments(words, segment, options, cancellationToken);
    }

    /// <summary>
    /// Handles the edge case where a segment contains only a single word that exceeds token limits.
    /// </summary>
    private static List<(string text, int startIndex, int endIndex)> HandleSingleWordSegment(
        (string text, int startIndex, int endIndex) segment)
    {
        // Single word that's too long - this is an edge case but we'll include it anyway
        // Log warning that this might cause embedding generation issues
        return new List<(string text, int startIndex, int endIndex)> { segment };
    }

    /// <summary>
    /// Processes words into segments that respect token limits.
    /// </summary>
    private async Task<List<(string text, int startIndex, int endIndex)>> ProcessWordsIntoSegments(
        string[] words,
        (string text, int startIndex, int endIndex) segment,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var result = new List<(string text, int startIndex, int endIndex)>();
        var currentSegmentWords = new List<string>();
        var currentStartIndex = segment.startIndex;

        for (int i = 0; i < words.Length; i++)
        {
            var (shouldCreateSegment, newStartIndex) = await ProcessNextWord(
                words[i], currentSegmentWords, segment, options, result, currentStartIndex, cancellationToken);

            if (shouldCreateSegment)
            {
                currentStartIndex = newStartIndex;
            }
        }

        AddFinalSegment(currentSegmentWords, result, currentStartIndex, segment.endIndex);
        return result;
    }

    /// <summary>
    /// Processes the next word and determines if a segment should be created.
    /// </summary>
    private async Task<(bool shouldCreateSegment, int newStartIndex)> ProcessNextWord(
        string word,
        List<string> currentSegmentWords,
        (string text, int startIndex, int endIndex) segment,
        SemanticChunkingOptions options,
        List<(string text, int startIndex, int endIndex)> result,
        int currentStartIndex,
        CancellationToken cancellationToken)
    {
        currentSegmentWords.Add(word);
        var testText = string.Join(" ", currentSegmentWords);
        var tokenCount = await _tokenCounter.CountTokensAsync(testText, cancellationToken);

        if (tokenCount > options.MaxTokensPerChunk)
        {
            return HandleTokenLimitExceeded(word, currentSegmentWords, segment, result, currentStartIndex);
        }

        return (false, currentStartIndex);
    }

    /// <summary>
    /// Handles the case when adding a word would exceed the token limit.
    /// </summary>
    private static (bool shouldCreateSegment, int newStartIndex) HandleTokenLimitExceeded(
        string word,
        List<string> currentSegmentWords,
        (string text, int startIndex, int endIndex) segment,
        List<(string text, int startIndex, int endIndex)> result,
        int currentStartIndex)
    {
        if (currentSegmentWords.Count > 1)
        {
            return CreateSegmentFromWords(word, currentSegmentWords, segment, result, currentStartIndex);
        }
        
        return HandleSingleWordExceedsLimit(currentSegmentWords, result, currentStartIndex);
    }

    /// <summary>
    /// Creates a segment from accumulated words and starts a new segment.
    /// </summary>
    private static (bool shouldCreateSegment, int newStartIndex) CreateSegmentFromWords(
        string currentWord,
        List<string> currentSegmentWords,
        (string text, int startIndex, int endIndex) segment,
        List<(string text, int startIndex, int endIndex)> result,
        int currentStartIndex)
    {
        // Remove the last word and create a segment
        currentSegmentWords.RemoveAt(currentSegmentWords.Count - 1);
        var segmentText = string.Join(" ", currentSegmentWords);

        var (actualStartIndex, endIndex) = FindSegmentPosition(segmentText, segment, currentStartIndex);
        result.Add((segmentText, actualStartIndex, endIndex));

        // Start new segment with the current word
        currentSegmentWords.Clear();
        currentSegmentWords.Add(currentWord);
        var newStartIndex = endIndex + 1;

        return (true, newStartIndex);
    }

    /// <summary>
    /// Handles the case where a single word exceeds the token limit.
    /// </summary>
    private static (bool shouldCreateSegment, int newStartIndex) HandleSingleWordExceedsLimit(
        List<string> currentSegmentWords,
        List<(string text, int startIndex, int endIndex)> result,
        int currentStartIndex)
    {
        // Single word is too long - add it anyway (edge case)
        var singleWordText = currentSegmentWords[0];
        result.Add((singleWordText, currentStartIndex, currentStartIndex + singleWordText.Length));
        currentSegmentWords.Clear();
        return (true, currentStartIndex + singleWordText.Length + 1);
    }

    /// <summary>
    /// Finds the actual position of a segment in the original text.
    /// </summary>
    private static (int startIndex, int endIndex) FindSegmentPosition(
        string segmentText,
        (string text, int startIndex, int endIndex) segment,
        int fallbackStartIndex)
    {
        var actualStartIndex = segment.text.IndexOf(segmentText, StringComparison.Ordinal);
        if (actualStartIndex >= 0)
        {
            actualStartIndex += segment.startIndex;
            return (actualStartIndex, actualStartIndex + segmentText.Length);
        }
        
        // Fallback to approximate positioning
        return (fallbackStartIndex, fallbackStartIndex + segmentText.Length);
    }

    /// <summary>
    /// Adds any remaining words as the final segment.
    /// </summary>
    private static void AddFinalSegment(
        List<string> currentSegmentWords,
        List<(string text, int startIndex, int endIndex)> result,
        int currentStartIndex,
        int segmentEndIndex)
    {
        if (currentSegmentWords.Count > 0)
        {
            var finalSegmentText = string.Join(" ", currentSegmentWords);
            result.Add((finalSegmentText, currentStartIndex, Math.Min(currentStartIndex + finalSegmentText.Length, segmentEndIndex)));
        }
    }

    /// <summary>
    /// Validates that segment groups don't exceed token limits for embedding generation.
    /// </summary>
    /// <param name="sentenceGroups">The sentence groups to validate.</param>
    /// <param name="options">The chunking options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of validated sentence groups.</returns>
    private async Task<List<SentenceGroup>> ValidateSegmentGroups(
        List<SentenceGroup> sentenceGroups,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var validatedGroups = new List<SentenceGroup>();

        foreach (var group in sentenceGroups)
        {
            var tokenCount = await _tokenCounter.CountTokensAsync(group.CombinedText, cancellationToken);

            if (tokenCount <= options.MaxTokensPerChunk)
            {
                // Group is within limits
                validatedGroups.Add(group);
            }
            else
            {
                // Group exceeds limits, create a simplified group with just the core sentence
                var coreSegment = group.Sentences.Count > 0 ? group.Sentences[0] : string.Empty;
                if (!string.IsNullOrEmpty(coreSegment))
                {
                    var coreTokenCount = await _tokenCounter.CountTokensAsync(coreSegment, cancellationToken);
                    if (coreTokenCount <= options.MaxTokensPerChunk)
                    {
                        // Use just the core segment without buffer context
                        var simplifiedGroup = new SentenceGroup(new List<string> { coreSegment }, group.StartIndex, group.EndIndex);
                        validatedGroups.Add(simplifiedGroup);
                    }
                    // If even the core segment is too large, we skip it (it should have been handled in ValidateAndSplitSegments)
                }
            }
        }

        return validatedGroups;
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
            var embeddingsList = new List<Vector<double>>();

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
        {
            return new List<int>();
        }

        var threshold = VectorExtensions.CalculatePercentile(distances, percentileThreshold);
        var breakpoints = VectorExtensions.FindBreakpoints(distances, threshold).ToList();

        return breakpoints;
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
        var validChunks = await ProcessBreakpointsToChunks(segments, breakpoints, metadata, options, cancellationToken);

        // If no valid chunks were created, create fallback chunks
        if (validChunks.Count == 0 && segments.Count > 0)
        {
            var fallbackChunks = await CreateFallbackChunks(segments, metadata, options, cancellationToken);
            validChunks.AddRange(fallbackChunks);
        }

        // Return all valid chunks
        foreach (var chunk in validChunks)
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Processes breakpoints to create valid chunks.
    /// </summary>
    private async Task<List<TextChunk>> ProcessBreakpointsToChunks(List<(string text, int startIndex, int endIndex)> segments,
        List<int> breakpoints,
        IDictionary<string, object>? metadata,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var chunkStartIndex = 0;
        var allBreakpoints = breakpoints.Concat(new[] { segments.Count - 1 }).Where(bp => bp >= 0).ToList();
        var chunks = new List<TextChunk?>();

        foreach (var breakpoint in allBreakpoints)
        {
            if (breakpoint >= chunkStartIndex)
            {
                var chunk = await TryCreateChunkFromSegments(segments, chunkStartIndex, breakpoint, metadata, options, cancellationToken);
                chunks.Add(chunk);
                chunkStartIndex = breakpoint + 1;
            }
        }

        return chunks.Where(c => c != null).Cast<TextChunk>().ToList();
    }

    /// <summary>
    /// Attempts to create a chunk from a range of segments.
    /// </summary>
    private async Task<TextChunk?> TryCreateChunkFromSegments(List<(string text, int startIndex, int endIndex)> segments,
        int startIndex,
        int endIndex,
        IDictionary<string, object>? metadata,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var chunkSegments = segments.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        if (chunkSegments.Count == 0)
        {
            return null;
        }

        var chunkText = string.Join(" ", chunkSegments.Select(s => s.text));
        var tokenCount = await _tokenCounter.CountTokensAsync(chunkText, cancellationToken);

        if (tokenCount >= options.MinTokensPerChunk && tokenCount <= options.MaxTokensPerChunk)
        {
            var chunkStartIndex = chunkSegments[0].startIndex;
            var chunkEndIndex = chunkSegments[chunkSegments.Count - 1].endIndex;
            var chunkMetadata = CreateChunkMetadata(metadata, tokenCount, chunkSegments.Count, false);

            return new TextChunk(chunkText, chunkStartIndex, chunkEndIndex, chunkMetadata);
        }

        return null;
    }

    /// <summary>
    /// Creates fallback chunks when no valid chunks were created from breakpoints.
    /// </summary>
    private async Task<List<TextChunk>> CreateFallbackChunks(List<(string text, int startIndex, int endIndex)> segments,
        IDictionary<string, object>? metadata,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var fallbackChunk = await TryCreateSingleFallbackChunk(segments, metadata, options, cancellationToken);
        if (fallbackChunk != null)
        {
            return new List<TextChunk> { fallbackChunk };
        }

        return await CreateSentenceByChunkFallback(segments, metadata, options, cancellationToken);
    }

    /// <summary>
    /// Attempts to create a single chunk with all segments as fallback.
    /// </summary>
    private async Task<TextChunk?> TryCreateSingleFallbackChunk(List<(string text, int startIndex, int endIndex)> segments,
        IDictionary<string, object>? metadata,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var allText = string.Join(" ", segments.Select(s => s.text));
        var totalTokenCount = await _tokenCounter.CountTokensAsync(allText, cancellationToken);

        if (totalTokenCount <= options.MaxTokensPerChunk)
        {
            var chunkMetadata = CreateChunkMetadata(metadata, totalTokenCount, segments.Count, true);
            return new TextChunk(allText, segments[0].startIndex, segments[segments.Count - 1].endIndex, chunkMetadata);
        }

        return null;
    }

    /// <summary>
    /// Creates sentence-by-sentence chunks as last resort fallback.
    /// </summary>
    private async Task<List<TextChunk>> CreateSentenceByChunkFallback(List<(string text, int startIndex, int endIndex)> segments,
        IDictionary<string, object>? metadata,
        SemanticChunkingOptions options,
        CancellationToken cancellationToken)
    {
        var fallbackChunks = new List<TextChunk>();

        foreach (var (text, startIndex, endIndex) in segments)
        {
            var tokenCount = await _tokenCounter.CountTokensAsync(text, cancellationToken);
            if (tokenCount <= options.MaxTokensPerChunk)
            {
                var chunkMetadata = CreateChunkMetadata(metadata, tokenCount, 1, true);
                fallbackChunks.Add(new TextChunk(text, startIndex, endIndex, chunkMetadata));
            }
        }

        return fallbackChunks;
    }

    /// <summary>
    /// Creates metadata for a text chunk.
    /// </summary>
    private static Dictionary<string, object> CreateChunkMetadata(IDictionary<string, object>? baseMetadata,
        int tokenCount,
        int segmentCount,
        bool isFallback)
    {
        var chunkMetadata = baseMetadata != null ? new Dictionary<string, object>(baseMetadata) : new Dictionary<string, object>();
        chunkMetadata["TokenCount"] = tokenCount;
        chunkMetadata["SegmentCount"] = segmentCount;
        if (isFallback)
        {
            chunkMetadata["IsFallback"] = true;
        }
        return chunkMetadata;
    }
}