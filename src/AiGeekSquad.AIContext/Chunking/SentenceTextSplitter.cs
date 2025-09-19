using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Chunking;

/// <summary>
/// A text splitter that splits text into sentences using regular expressions.
/// The default implementation is optimized for English text and handles common English titles
/// and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.) to avoid incorrect sentence breaks.
/// </summary>
public class SentenceTextSplitter : ITextSplitter
{
    private readonly Regex _sentencePattern;
    private readonly bool _markdownMode;
    private readonly TimeSpan _regexTimeout;
    private const string DefaultPattern = @"(?<!Mr\.)(?<!Mrs\.)(?<!Ms\.)(?<!Dr\.)(?<!Prof\.)(?<!Sr\.)(?<!Jr\.)(?<=[.!?])\s+(?=[A-Z])";

    /// <summary>
    /// Initializes a new instance of the <see cref="SentenceTextSplitter"/> class.
    /// </summary>
    /// <param name="pattern">Optional custom regex pattern for sentence splitting. If null, uses default pattern
    /// that handles common English titles and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.).</param>
    /// <param name="markdownMode">If true, enables markdown-aware splitting (preserves code blocks, lists, headers, inline code, links/images).</param>
    /// <param name="regexTimeout">Optional timeout for regex operations. If null, defaults to 10 seconds. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when regexTimeout is zero or negative.</exception>
    public SentenceTextSplitter(string? pattern = null, bool markdownMode = false, TimeSpan? regexTimeout = null)
    {
        _regexTimeout = regexTimeout ?? TimeSpan.FromSeconds(10);

        // Validate timeout parameter
        if (_regexTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(regexTimeout), "Regex timeout must be positive.");
        }

        _sentencePattern = new Regex(pattern ?? DefaultPattern, RegexOptions.Compiled, _regexTimeout);
        _markdownMode = markdownMode;
    }

    /// <summary>
    /// Asynchronously splits the specified text into sentence segments.
    /// If markdown mode is enabled, preserves markdown blocks, lists, headers, inline code, links, and images as atomic segments.
    /// </summary>
    public async IAsyncEnumerable<TextSegment> SplitAsync(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        await foreach (var segment in SplitAsyncCore(text, cancellationToken))
        {
            yield return segment;
        }
    }

    /// <summary>
    /// Core implementation for asynchronously splitting text into sentence segments.
    /// </summary>
    private async IAsyncEnumerable<TextSegment> SplitAsyncCore(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_markdownMode)
        {
            foreach (var segment in MarkdownAwareSplit(text, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return segment;
                await Task.Yield();
            }
            yield break;
        }

        // Split text into sentences using the regex pattern
        // Removed .ToArray() call to avoid unnecessary memory allocation
        var sentenceBoundaries = _sentencePattern.Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s));

        var currentIndex = 0;
        foreach (var sentence in sentenceBoundaries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmedSentence = sentence.Trim();
            if (!string.IsNullOrEmpty(trimmedSentence))
            {
                var startIndex = text.IndexOf(trimmedSentence, currentIndex, StringComparison.Ordinal);
                if (startIndex >= 0)
                {
                    var endIndex = startIndex + trimmedSentence.Length;
                    yield return new TextSegment(trimmedSentence, startIndex, endIndex);
                    currentIndex = endIndex;
                }
            }

            await Task.Yield();
        }

        // Check if no valid sentences were found by converting to list only when needed
        var boundariesList = sentenceBoundaries.ToList();
        if (boundariesList.Count == 0 || boundariesList.All(string.IsNullOrWhiteSpace))
        {
            var trimmedText = text.Trim();
            if (!string.IsNullOrEmpty(trimmedText))
            {
                yield return new TextSegment(trimmedText, 0, text.Length);
            }
        }
    }

    /// <summary>
    /// Comprehensive markdown-aware splitting that handles all edge cases.
    /// </summary>
    private IEnumerable<TextSegment> MarkdownAwareSplit(string text, CancellationToken cancellationToken)
    {
        // Step 1: Preprocess mixed content patterns
        var processedText = PreprocessMixedContent(text);

        // Step 2: Parse with Markdig
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(processedText, pipeline);

        // Step 3: Extract segments with proper handling for each block type
        var segments = new List<TextSegment>();

        foreach (var block in document)
        {
            cancellationToken.ThrowIfCancellationRequested();
            segments.AddRange(ExtractSegmentsFromBlock(block, processedText, text));
        }

        // Step 4: Handle any remaining unprocessed text
        var processedSegments = HandleUnprocessedText(segments);

        return processedSegments.OrderBy(s => s.StartIndex);
    }


    /// <summary>
    /// Preprocesses text to handle mixed content patterns that Markdig doesn't parse correctly.
    /// </summary>
    private string PreprocessMixedContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var result = text;

        // Pattern 1: "sentence. - list item" -> "sentence.\n- list item"
        result = Regex.Replace(result, @"([.!?])\s+([-*+])\s", "$1\n$2 ", RegexOptions.None, _regexTimeout);

        // Pattern 2: "sentence.\nAnother sentence" after list items
        result = Regex.Replace(result, @"([-*+]\s+[^\n]*)\n([A-Z][^-*+\n]*[.!?])", "$1\n\n$2", RegexOptions.None, _regexTimeout);

        return result;
    }

    /// <summary>
    /// Extracts segments from a markdown block with comprehensive handling.
    /// </summary>
    private IEnumerable<TextSegment> ExtractSegmentsFromBlock(Block block, string processedText, string originalText)
    {
        var blockStart = Math.Max(0, Math.Min(block.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(block.Span.End + 1, processedText.Length));

        Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Block type: {block.GetType().Name}, Span: {block.Span.Start}-{block.Span.End}");

        if (blockStart >= blockEnd)
        {
            Debug.WriteLine("[DEBUG] ExtractSegmentsFromBlock - Invalid span, skipping block");
            yield break;
        }

        switch (block)
        {
            case ListBlock listBlock:
                Debug.WriteLine("[DEBUG] ExtractSegmentsFromBlock - Processing ListBlock");
                foreach (var segment in ExtractListSegments(listBlock, processedText, originalText))
                {
                    yield return segment;
                }
                break;

            case HeadingBlock headingBlock:
                Debug.WriteLine("[DEBUG] ExtractSegmentsFromBlock - Processing HeadingBlock");
                foreach (var segment in ExtractHeadingSegments(headingBlock, processedText, originalText))
                {
                    yield return segment;
                }
                break;

            case CodeBlock codeBlock:
                Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing CodeBlock ({codeBlock.GetType().Name})");
                foreach (var segment in ExtractCodeBlockSegments(codeBlock, processedText, originalText))
                {
                    yield return segment;
                }
                break;

            case ParagraphBlock paragraphBlock:
                Debug.WriteLine("[DEBUG] ExtractSegmentsFromBlock - Processing ParagraphBlock");
                foreach (var segment in ExtractParagraphSegments(paragraphBlock, processedText, originalText))
                {
                    yield return segment;
                }
                break;

            case QuoteBlock quoteBlock:
                Debug.WriteLine("[DEBUG] ExtractSegmentsFromBlock - Processing QuoteBlock");
                foreach (var segment in ExtractQuoteSegments(quoteBlock, processedText, originalText))
                {
                    yield return segment;
                }
                break;

            default:
                Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing unknown block type: {block.GetType().Name}");
                // Fallback for other block types
                foreach (var segment in ExtractFallbackSegments(block, processedText, originalText))
                {
                    yield return segment;
                }
                break;
        }
    }

    /// <summary>
    /// Extracts segments from list blocks, handling nested lists properly.
    /// </summary>
    private static IEnumerable<TextSegment> ExtractListSegments(ListBlock listBlock, string processedText, string originalText)
    {
        return listBlock.OfType<ListItemBlock>()
            .SelectMany(listItemBlock => ProcessListItem(listItemBlock, processedText, originalText));
    }

    /// <summary>
    /// Processes a single list item and extracts its text segments.
    /// </summary>
    private static IEnumerable<TextSegment> ProcessListItem(ListItemBlock listItemBlock, string processedText, string originalText)
    {
        var itemStart = Math.Max(0, Math.Min(listItemBlock.Span.Start, processedText.Length));
        var itemEnd = Math.Max(itemStart, Math.Min(listItemBlock.Span.End + 1, processedText.Length));

        if (itemStart >= itemEnd)
        {
            yield break;
        }

        var itemText = processedText.Substring(itemStart, itemEnd - itemStart);
        var lines = itemText.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // For list items, preserve indentation when needed
            var originalSegment = FindInOriginalTextWithIndentation(trimmedLine, originalText, itemStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
    }

    /// <summary>
    /// Extracts segments from heading blocks.
    /// </summary>
    private static IEnumerable<TextSegment> ExtractHeadingSegments(HeadingBlock headingBlock, string processedText, string originalText)
    {
        var blockStart = Math.Max(0, Math.Min(headingBlock.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(headingBlock.Span.End + 1, processedText.Length));

        if (blockStart >= blockEnd)
        {
            yield break;
        }

        var headingText = processedText.Substring(blockStart, blockEnd - blockStart).Trim();

        // Handle empty headings - preserve trailing space
        if (string.IsNullOrEmpty(headingText.Replace("#", "").Trim()))
        {
            var originalHeading = processedText.Substring(blockStart, blockEnd - blockStart);
            var originalSegment = FindInOriginalTextPreservingWhitespace(originalHeading, originalText, blockStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
        else
        {
            var originalSegment = FindInOriginalText(headingText, originalText, blockStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
    }

    /// <summary>
    /// Extracts segments from code blocks, preserving indentation and whitespace.
    /// For fenced code blocks, reconstructs the original markdown syntax with delimiters.
    /// </summary>
    private static IEnumerable<TextSegment> ExtractCodeBlockSegments(CodeBlock codeBlock, string processedText, string originalText)
    {
        if (codeBlock is FencedCodeBlock fencedCodeBlock)
        {
            return ExtractFencedCodeBlockSegments(fencedCodeBlock, originalText);
        }

        return ExtractIndentedCodeBlockSegments(codeBlock, processedText, originalText);
    }

    /// <summary>
    /// Extracts segments from fenced code blocks (```code```).
    /// </summary>
    private static IEnumerable<TextSegment> ExtractFencedCodeBlockSegments(FencedCodeBlock fencedCodeBlock, string originalText)
    {
        var fence = new string(fencedCodeBlock.FencedChar, fencedCodeBlock.OpeningFencedCharCount);
        var info = fencedCodeBlock.Info ?? string.Empty;
        var contentLines = ExtractContentLinesFromFencedBlock(fencedCodeBlock);

        // Try both line ending styles to match the original text
        var originalSegment = TryFindFencedBlockInOriginalText(fence, info, contentLines, originalText);
        if (originalSegment != null)
        {
            yield return originalSegment;
        }
    }

    /// <summary>
    /// Extracts content lines from a fenced code block.
    /// </summary>
    private static List<string> ExtractContentLinesFromFencedBlock(FencedCodeBlock fencedCodeBlock)
    {
        var contentLines = new List<string>();
        if (fencedCodeBlock.Lines.Count > 0)
        {
            for (var i = 0; i < fencedCodeBlock.Lines.Count; i++)
            {
                var line = fencedCodeBlock.Lines.Lines[i];
                contentLines.Add(line.ToString());
            }
        }
        return contentLines;
    }

    /// <summary>
    /// Tries to find a fenced code block in the original text using different line ending styles.
    /// </summary>
    private static TextSegment? TryFindFencedBlockInOriginalText(string fence, string info, List<string> contentLines, string originalText)
    {
        // Try both \n and \r\n line endings to match the original text
        var reconstructedWithLF = ReconstructFencedCodeBlock(fence, info, contentLines, "\n");
        var reconstructedWithCRLF = ReconstructFencedCodeBlock(fence, info, contentLines, "\r\n");

        // Try to find either variant in the original text
        return FindInOriginalTextPreservingWhitespace(reconstructedWithCRLF, originalText)
               ?? FindInOriginalTextPreservingWhitespace(reconstructedWithLF, originalText);
    }

    /// <summary>
    /// Extracts segments from indented code blocks (4+ spaces or tab indentation).
    /// </summary>
    private static IEnumerable<TextSegment> ExtractIndentedCodeBlockSegments(CodeBlock codeBlock, string processedText, string originalText)
    {
        var (blockStart, blockEnd) = GetValidBlockBounds(codeBlock, processedText);
        if (blockStart >= blockEnd)
        {
            yield break;
        }

        var codeText = processedText.Substring(blockStart, blockEnd - blockStart);
        var lines = codeText.Split('\n');

        foreach (var line in lines)
        {
            var originalSegment = ProcessIndentedCodeLine(line, originalText, blockStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
    }

    /// <summary>
    /// Gets valid bounds for a code block, ensuring they're within the processed text length.
    /// </summary>
    private static (int start, int end) GetValidBlockBounds(CodeBlock codeBlock, string processedText)
    {
        var blockStart = Math.Max(0, Math.Min(codeBlock.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(codeBlock.Span.End + 1, processedText.Length));
        return (blockStart, blockEnd);
    }

    /// <summary>
    /// Processes a single line from an indented code block.
    /// </summary>
    private static TextSegment? ProcessIndentedCodeLine(string line, string originalText, int blockStart)
    {
        var trimmedLine = line.TrimEnd('\r');
        if (string.IsNullOrEmpty(trimmedLine))
        {
            return null;
        }

        // For indented code blocks, preserve the original indentation
        return FindInOriginalTextWithIndentation(trimmedLine, originalText, blockStart);
    }

    /// <summary>
    /// Helper method to reconstruct a fenced code block with the specified line ending.
    /// </summary>
    private static string ReconstructFencedCodeBlock(string fence, string info, List<string> contentLines, string lineEnding)
    {
        var reconstructed = new List<string> { fence + info };
        reconstructed.AddRange(contentLines);
        reconstructed.Add(fence);

        return string.Join(lineEnding, reconstructed);
    }

    /// <summary>
    /// Extracts segments from paragraph blocks, handling inline elements properly.
    /// </summary>
    private IEnumerable<TextSegment> ExtractParagraphSegments(ParagraphBlock paragraphBlock, string processedText, string originalText)
    {
        var blockStart = Math.Max(0, Math.Min(paragraphBlock.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(paragraphBlock.Span.End + 1, processedText.Length));

        if (blockStart >= blockEnd)
        {
            yield break;
        }

        var paraText = processedText.Substring(blockStart, blockEnd - blockStart);
        var inlines = paragraphBlock.Inline?.ToList() ?? [];

        // Check if paragraph contains inline code
        var hasInlineCode = inlines.Any(i => i is CodeInline);

        if (hasInlineCode)
        {
            // Keep paragraphs with inline code as atomic segments
            var originalSegment = FindInOriginalText(paraText.Trim(), originalText, blockStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
        else
        {
            // Regular paragraph - split by sentences
            var sentences = _sentencePattern.Split(paraText).Where(s => !string.IsNullOrWhiteSpace(s));
            foreach (var sentence in sentences)
            {
                var trimmedSentence = sentence.Trim();
                if (!string.IsNullOrEmpty(trimmedSentence))
                {
                    var originalSegment = FindInOriginalText(trimmedSentence, originalText, blockStart);
                    if (originalSegment != null)
                    {
                        yield return originalSegment;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts segments from quote blocks.
    /// </summary>
    private static IEnumerable<TextSegment> ExtractQuoteSegments(QuoteBlock quoteBlock, string processedText, string originalText)
    {
        var blockStart = Math.Max(0, Math.Min(quoteBlock.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(quoteBlock.Span.End + 1, processedText.Length));

        if (blockStart >= blockEnd)
        {
            yield break;
        }

        var quoteText = processedText.Substring(blockStart, blockEnd - blockStart);
        var lines = quoteText.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            var originalSegment = FindInOriginalText(trimmedLine, originalText, blockStart);
            if (originalSegment != null)
            {
                yield return originalSegment;
            }
        }
    }

    /// <summary>
    /// Fallback segment extraction for unhandled block types.
    /// </summary>
    private static IEnumerable<TextSegment> ExtractFallbackSegments(Block block, string processedText, string originalText)
    {
        var blockStart = Math.Max(0, Math.Min(block.Span.Start, processedText.Length));
        var blockEnd = Math.Max(blockStart, Math.Min(block.Span.End + 1, processedText.Length));

        if (blockStart >= blockEnd) yield break;

        var blockText = processedText.Substring(blockStart, blockEnd - blockStart);

        // Handle empty elements specially
        if (string.IsNullOrEmpty(blockText.Trim()))
        {
            var originalSegment = FindInOriginalTextPreservingWhitespace(blockText, originalText, blockStart);
            if (originalSegment != null)
                yield return originalSegment;
            yield break;
        }

        var lines = blockText.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

            var originalSegment = FindInOriginalText(trimmedLine, originalText, blockStart);
            if (originalSegment != null)
                yield return originalSegment;
        }
    }

    /// <summary>
    /// Finds text in the original string and creates a TextSegment with correct positions.
    /// </summary>
    private static TextSegment? FindInOriginalText(string text, string originalText, int searchStartHint = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var trimmedText = text.Trim();
        if (string.IsNullOrEmpty(trimmedText))
        {
            return null;
        }

        // Ensure searchStartHint is within bounds of originalText
        var safeSearchStart = Math.Max(0, Math.Min(searchStartHint, originalText.Length));
        var startIndex = originalText.IndexOf(trimmedText, safeSearchStart, StringComparison.Ordinal);
        if (startIndex >= 0)
        {
            return new TextSegment(trimmedText, startIndex, startIndex + trimmedText.Length);
        }

        // Fallback: try from beginning
        startIndex = originalText.IndexOf(trimmedText, StringComparison.Ordinal);
        if (startIndex >= 0)
        {
            return new TextSegment(trimmedText, startIndex, startIndex + trimmedText.Length);
        }

        return null;
    }

    /// <summary>
    /// Finds text with indentation preserved (for code blocks and lists).
    /// </summary>
    private static TextSegment? FindInOriginalTextWithIndentation(string text, string originalText, int searchStartHint = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // For indented content, preserve the original indentation
        var lines = originalText.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Trim() == text.Trim() && line.Contains(text.Trim()))
            {
                var startIndex = originalText.IndexOf(line, StringComparison.Ordinal);
                if (startIndex >= 0)
                {
                    return new TextSegment(line, startIndex, startIndex + line.Length);
                }
            }
        }

        // Fallback to regular search
        return FindInOriginalText(text, originalText, searchStartHint);
    }

    /// <summary>
    /// Finds text preserving all whitespace (for empty elements).
    /// </summary>
    private static TextSegment? FindInOriginalTextPreservingWhitespace(string text, string originalText, int searchStartHint = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // Ensure searchStartHint is within bounds of originalText
        var safeSearchStart = Math.Max(0, Math.Min(searchStartHint, originalText.Length));
        var startIndex = originalText.IndexOf(text, safeSearchStart, StringComparison.Ordinal);
        if (startIndex >= 0)
        {
            return new TextSegment(text, startIndex, startIndex + text.Length);
        }

        return null;
    }

    /// <summary>
    /// Handles any text that wasn't processed by markdown parsing.
    /// </summary>
    private static List<TextSegment> HandleUnprocessedText(List<TextSegment> segments)
    {
        // For now, just return the segments as-is
        // Could be extended to handle edge cases where some text is missed
        return segments;
    }

    /// <summary>
    /// Creates a sentence splitter with a custom regex pattern.
    /// </summary>
    /// <param name="pattern">The regex pattern to use for sentence splitting.</param>
    /// <param name="regexTimeout">Optional timeout for regex operations. If null, defaults to 10 seconds.</param>
    /// <returns>A new instance of <see cref="SentenceTextSplitter"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
    public static SentenceTextSplitter WithPattern(string pattern, TimeSpan? regexTimeout = null)
    {
        return string.IsNullOrWhiteSpace(pattern) ? throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern)) : new SentenceTextSplitter(pattern, regexTimeout: regexTimeout);
    }

    /// <summary>
    /// Creates a sentence splitter with the default sentence detection pattern.
    /// The default pattern is optimized for English text and handles common English titles
    /// and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.) to prevent incorrect sentence breaks.
    /// </summary>
    /// <returns>A new instance of <see cref="SentenceTextSplitter"/> with default settings.</returns>
    public static SentenceTextSplitter Default => new SentenceTextSplitter();

    /// <summary>
    /// Creates a sentence splitter with markdown-aware splitting enabled.
    /// </summary>
    /// <param name="regexTimeout">Optional timeout for regex operations. If null, defaults to 10 seconds.</param>
    /// <returns>A new instance of <see cref="SentenceTextSplitter"/> with markdown mode enabled.</returns>
    public static SentenceTextSplitter ForMarkdown(TimeSpan? regexTimeout = null)
    {
        return new SentenceTextSplitter(null, true, regexTimeout);
    }

    /// <summary>
    /// Creates a sentence splitter with a custom pattern and markdown-aware splitting enabled.
    /// </summary>
    /// <param name="pattern">The regex pattern to use for sentence splitting.</param>
    /// <param name="regexTimeout">Optional timeout for regex operations. If null, defaults to 10 seconds.</param>
    /// <returns>A new instance of <see cref="SentenceTextSplitter"/> with markdown mode enabled.</returns>
    /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
    public static SentenceTextSplitter WithPatternForMarkdown(string pattern, TimeSpan? regexTimeout = null)
    {
        return string.IsNullOrWhiteSpace(pattern) ? throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern)) : new SentenceTextSplitter(pattern, true, regexTimeout);
    }
}