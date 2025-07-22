using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// A text splitter that splits text into sentences using regular expressions.
    /// The default implementation is optimized for English text and handles common English titles
    /// and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.) to avoid incorrect sentence breaks.
    /// </summary>
    public class SentenceTextSplitter : ITextSplitter
    {
        private readonly Regex _sentencePattern;
        private readonly bool _markdownMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceTextSplitter"/> class.
        /// </summary>
        /// <param name="pattern">Optional custom regex pattern for sentence splitting. If null, uses default pattern
        /// that handles common English titles and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.).</param>
        /// <param name="markdownMode">If true, enables markdown-aware splitting (preserves code blocks, lists, headers, inline code, links/images).</param>
        public SentenceTextSplitter(string? pattern = null, bool markdownMode = false)
        {
            var defaultPattern = @"(?<!Mr\.)(?<!Mrs\.)(?<!Ms\.)(?<!Dr\.)(?<!Prof\.)(?<!Sr\.)(?<!Jr\.)(?<=[.!?])\s+(?=[A-Z])";
            _sentencePattern = new Regex(pattern ?? defaultPattern, RegexOptions.Compiled);
            _markdownMode = markdownMode;
        }

        /// <summary>
        /// Asynchronously splits the specified text into sentence segments.
        /// </summary>
        /// <param name="text">The text to split into sentences.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of text segments representing sentences.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        /// <summary>
        /// Asynchronously splits the specified text into sentence segments.
        /// If markdown mode is enabled, preserves markdown blocks, lists, headers, inline code, links, and images as atomic segments.
        /// </summary>
        public async IAsyncEnumerable<TextSegment> SplitAsync(
            string text,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            if (_markdownMode)
            {
                foreach (var segment in MarkdownAwareSplit(text, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return segment;
                    await System.Threading.Tasks.Task.Yield();
                }
                yield break;
            }

            // Split text into sentences using the regex pattern
            var sentenceBoundaries = _sentencePattern.Split(text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

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

                await System.Threading.Tasks.Task.Yield();
            }

            if (sentenceBoundaries.Length == 0 || sentenceBoundaries.All(string.IsNullOrWhiteSpace))
            {
                var trimmedText = text.Trim();
                if (!string.IsNullOrEmpty(trimmedText))
                {
                    yield return new TextSegment(trimmedText, 0, text.Length);
                }
            }
        }

        /// <summary>
        /// Markdown-aware splitting: uses Markdig to parse markdown and preserve block boundaries.
        /// </summary>
        private IEnumerable<TextSegment> MarkdownAwareSplit(string text, CancellationToken cancellationToken)
        {
            var pipeline = new MarkdownPipelineBuilder().Build();
            var document = Markdown.Parse(text, pipeline);

            foreach (var block in document)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var blockStart = block.Span.Start;
                var blockEnd = block.Span.End + 1;

                if (block is ListBlock list)
                {
                    foreach (var segment in ExtractListItemsRecursive(list, text))
                        yield return segment;
                    continue;
                }

                if (block is HeadingBlock heading)
                {
                    var headingText = text.Substring(blockStart, blockEnd - blockStart);
                    yield return new TextSegment(headingText, blockStart, blockEnd);
                    continue;
                }

                if (block is CodeBlock code)
                {
                    var codeText = text.Substring(blockStart, blockEnd - blockStart);
                    yield return new TextSegment(codeText, blockStart, blockEnd);
                    continue;
                }

                if (block is QuoteBlock quote)
                {
                    var quoteText = text.Substring(blockStart, blockEnd - blockStart);
                    foreach (var line in quoteText.Split('\n'))
                    {
                        var trimmedLine = line.TrimEnd('\r');
                        if (!string.IsNullOrWhiteSpace(trimmedLine))
                        {
                            var lineStart = text.IndexOf(trimmedLine, blockStart, StringComparison.Ordinal);
                            var lineEnd = lineStart + trimmedLine.Length;
                            yield return new TextSegment(trimmedLine, lineStart, lineEnd);
                        }
                    }
                    continue;
                }

                if (block is ParagraphBlock para)
                {
                    var paraText = text.Substring(blockStart, blockEnd - blockStart);
                    var inlines = para.Inline?.ToList() ?? new List<Inline>();
                    
                    // Check if this contains malformed markdown (lines starting with -, *, #, etc.)
                    var paraLines = paraText.Split('\n');
                    bool hasMalformedMarkdown = paraLines.Any(line =>
                    {
                        var trimmed = line.Trim();
                        return trimmed.StartsWith("-") || trimmed.StartsWith("*") ||
                               trimmed.StartsWith("+") || trimmed.StartsWith("#") ||
                               System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.");
                    });
                    
                    if (hasMalformedMarkdown)
                    {
                        // Split by lines for malformed markdown
                        foreach (var line in paraLines)
                        {
                            var trimmedLine = line.TrimEnd('\r');
                            if (!string.IsNullOrWhiteSpace(trimmedLine))
                            {
                                var lineStart = text.IndexOf(trimmedLine, blockStart, StringComparison.Ordinal);
                                var lineEnd = lineStart + trimmedLine.Length;
                                yield return new TextSegment(trimmedLine, lineStart, lineEnd);
                            }
                        }
                    }
                    else if (inlines.Any(i => i is CodeInline || i is LinkInline))
                    {
                        // Check if paragraph contains sentence boundaries
                        var sentences = _sentencePattern.Split(paraText).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                        if (sentences.Length <= 1)
                        {
                            // No sentence boundaries: yield entire paragraph as atomic
                            yield return new TextSegment(paraText, blockStart, blockEnd);
                        }
                        else
                        {
                            // Has sentence boundaries: split by sentence first, then extract inlines
                            foreach (var s in SplitParagraphSentences(paraText, blockStart))
                            {
                                // Check if this sentence contains inline code/links
                                var sentenceInlines = inlines.Where(inline =>
                                    inline.Span.Start >= s.StartIndex - blockStart &&
                                    inline.Span.End <= s.EndIndex - blockStart).ToList();
                                
                                if (sentenceInlines.Any(i => i is CodeInline || i is LinkInline))
                                {
                                    // Split out the inline elements from this sentence
                                    var sentenceText = s.Text;
                                    var sentenceStart = s.StartIndex;
                                    int lastEnd = 0;
                                    
                                    foreach (var inline in sentenceInlines.Where(i => i is CodeInline || i is LinkInline))
                                    {
                                        var relativeStart = inline.Span.Start - (sentenceStart - blockStart);
                                        var relativeEnd = inline.Span.End - (sentenceStart - blockStart);
                                        
                                        if (relativeStart > lastEnd)
                                        {
                                            var plainText = sentenceText.Substring(lastEnd, relativeStart - lastEnd);
                                            if (!string.IsNullOrWhiteSpace(plainText))
                                                yield return new TextSegment(plainText, sentenceStart + lastEnd, sentenceStart + relativeStart);
                                        }
                                        
                                        var inlineText = sentenceText.Substring(relativeStart, relativeEnd - relativeStart + 1);
                                        yield return new TextSegment(inlineText, sentenceStart + relativeStart, sentenceStart + relativeEnd + 1);
                                        lastEnd = relativeEnd + 1;
                                    }
                                    
                                    if (lastEnd < sentenceText.Length)
                                    {
                                        var remainingText = sentenceText.Substring(lastEnd);
                                        if (!string.IsNullOrWhiteSpace(remainingText))
                                            yield return new TextSegment(remainingText, sentenceStart + lastEnd, sentenceStart + sentenceText.Length);
                                    }
                                }
                                else
                                {
                                    yield return s;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Regular paragraph: split by sentence
                        foreach (var s in SplitParagraphSentences(paraText, blockStart))
                            yield return s;
                    }
                    continue;
                }

                // Fallback: split by line as atomic segments
                var blockText = text.Substring(blockStart, blockEnd - blockStart);
                var lines = blockText.Split('\n');
                if (lines.Length > 1)
                {
                    foreach (var line in lines)
                    {
                        var atomicLine = line.TrimEnd('\r');
                        if (!string.IsNullOrWhiteSpace(atomicLine))
                        {
                            var lineStart = text.IndexOf(atomicLine, blockStart, StringComparison.Ordinal);
                            var lineEnd = lineStart + atomicLine.Length;
                            yield return new TextSegment(atomicLine, lineStart, lineEnd);
                        }
                    }
                }
                else
                {
                    // If only one line, yield as atomic
                    var atomicLine = blockText.TrimEnd('\r');
                    if (!string.IsNullOrWhiteSpace(atomicLine))
                    {
                        yield return new TextSegment(atomicLine, blockStart, blockEnd);
                    }
                }
            }

            // Recursively extract all list items (including nested)
            IEnumerable<TextSegment> ExtractListItemsRecursive(ListBlock list, string text)
            {
                foreach (var item in list)
                {
                    if (item is ListItemBlock listItem)
                    {
                        var itemStart = listItem.Span.Start;
                        var itemEnd = listItem.Span.End + 1;
                        var itemText = text.Substring(itemStart, itemEnd - itemStart);
                        foreach (var line in itemText.Split('\n'))
                        {
                            var atomicLine = line.TrimEnd('\r');
                            if (!string.IsNullOrWhiteSpace(atomicLine))
                            {
                                var lineStart = text.IndexOf(atomicLine, itemStart, StringComparison.Ordinal);
                                var lineEnd = lineStart + atomicLine.Length;
                                yield return new TextSegment(atomicLine, lineStart, lineEnd);
                            }
                        }
                    }
                }
            }

            // Helper: split paragraph text by sentence, using offset for accurate indices
            IEnumerable<TextSegment> SplitParagraphSentences(string paraText, int offset)
            {
                var sentences = _sentencePattern.Split(paraText)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
                var idx = 0;
                foreach (var sentence in sentences)
                {
                    var trimmed = sentence.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        var localStart = paraText.IndexOf(trimmed, idx, StringComparison.Ordinal);
                        if (localStart >= 0)
                        {
                            var start = offset + localStart;
                            var end = start + trimmed.Length;
                            yield return new TextSegment(trimmed, start, end);
                            idx = localStart + trimmed.Length;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a sentence splitter with a custom regex pattern.
        /// </summary>
        /// <param name="pattern">The regex pattern to use for sentence splitting.</param>
        /// <returns>A new instance of <see cref="SentenceTextSplitter"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
        public static SentenceTextSplitter WithPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

            return new SentenceTextSplitter(pattern);
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
        /// <returns>A new instance of <see cref="SentenceTextSplitter"/> with markdown mode enabled.</returns>
        public static SentenceTextSplitter ForMarkdown()
        {
            return new SentenceTextSplitter(null, true);
        }

        /// <summary>
        /// Creates a sentence splitter with a custom pattern and markdown-aware splitting enabled.
        /// </summary>
        /// <param name="pattern">The regex pattern to use for sentence splitting.</param>
        /// <returns>A new instance of <see cref="SentenceTextSplitter"/> with markdown mode enabled.</returns>
        /// <exception cref="ArgumentException">Thrown when pattern is null or empty.</exception>
        public static SentenceTextSplitter WithPatternForMarkdown(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
            return new SentenceTextSplitter(pattern, true);
        }
    }
}