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
        /// Simple markdown-aware splitting: parse with Markdig and extract all components directly.
        /// </summary>
        private IEnumerable<TextSegment> MarkdownAwareSplit(string text, CancellationToken cancellationToken)
        {
            // Handle simple mixed patterns first (sentence. - list item)
            var mixedPattern = @"([.!?])\s+([-*+])\s";
            if (Regex.IsMatch(text, mixedPattern))
            {
                text = Regex.Replace(text, mixedPattern, "$1\n$2 ");
            }
            
            var pipeline = new MarkdownPipelineBuilder().Build();
            var document = Markdown.Parse(text, pipeline);

            foreach (var block in document)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var segment in ExtractSegmentsFromBlock(block, text))
                {
                    yield return segment;
                }
            }
        }

        private IEnumerable<TextSegment> ExtractSegmentsFromBlock(Block block, string text)
        {
            var blockStart = block.Span.Start;
            var blockEnd = Math.Min(block.Span.End + 1, text.Length);
            
            if (blockStart >= text.Length) yield break;
            if (blockEnd > text.Length) blockEnd = text.Length;
            if (blockEnd <= blockStart) yield break;

            var blockText = text.Substring(blockStart, blockEnd - blockStart);

            // Handle different block types
            switch (block)
            {
                case ListBlock list:
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            var itemStart = listItem.Span.Start;
                            var itemEnd = Math.Min(listItem.Span.End + 1, text.Length);
                            if (itemEnd > itemStart && itemStart < text.Length)
                            {
                                var itemText = text.Substring(itemStart, itemEnd - itemStart).Trim();
                                yield return new TextSegment(itemText, itemStart, itemEnd);
                            }
                        }
                    }
                    break;

                case HeadingBlock heading:
                case CodeBlock code:
                    yield return new TextSegment(blockText.Trim(), blockStart, blockEnd);
                    break;

                case ParagraphBlock para:
                    // Check for inline code or links - if found, split them out
                    var inlines = para.Inline?.ToList() ?? new List<Inline>();
                    if (inlines.Any(i => i is CodeInline))
                    {
                        // Split by inline code
                        var pattern = @"(`[^`]+`)";
                        var parts = Regex.Split(blockText, pattern);
                        var partIndex = blockStart;
                        
                        foreach (var part in parts)
                        {
                            if (string.IsNullOrWhiteSpace(part)) continue;
                            
                            var trimmedPart = part.Trim();
                            if (trimmedPart.StartsWith("`") && trimmedPart.EndsWith("`"))
                            {
                                // Inline code
                                var codeStart = text.IndexOf(trimmedPart, partIndex);
                                if (codeStart >= 0)
                                {
                                    yield return new TextSegment(trimmedPart, codeStart, codeStart + trimmedPart.Length);
                                    partIndex = codeStart + trimmedPart.Length;
                                }
                            }
                            else
                            {
                                // Regular text - split by sentences
                                var sentences = _sentencePattern.Split(trimmedPart).Where(s => !string.IsNullOrWhiteSpace(s));
                                foreach (var sentence in sentences)
                                {
                                    var trimmedSentence = sentence.Trim();
                                    if (!string.IsNullOrEmpty(trimmedSentence))
                                    {
                                        var sentStart = text.IndexOf(trimmedSentence, partIndex);
                                        if (sentStart >= 0)
                                        {
                                            yield return new TextSegment(trimmedSentence, sentStart, sentStart + trimmedSentence.Length);
                                            partIndex = sentStart + trimmedSentence.Length;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Regular paragraph - split by sentences
                        var sentences = _sentencePattern.Split(blockText).Where(s => !string.IsNullOrWhiteSpace(s));
                        var sentIndex = blockStart;
                        foreach (var sentence in sentences)
                        {
                            var trimmedSentence = sentence.Trim();
                            if (!string.IsNullOrEmpty(trimmedSentence))
                            {
                                var sentStart = text.IndexOf(trimmedSentence, sentIndex);
                                if (sentStart >= 0)
                                {
                                    yield return new TextSegment(trimmedSentence, sentStart, sentStart + trimmedSentence.Length);
                                    sentIndex = sentStart + trimmedSentence.Length;
                                }
                            }
                        }
                    }
                    break;

                default:
                    // Fallback for other block types
                    var lines = blockText.Split('\n');
                    var lineIndex = blockStart;
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            var lineStart = text.IndexOf(trimmedLine, lineIndex);
                            if (lineStart >= 0)
                            {
                                yield return new TextSegment(trimmedLine, lineStart, lineStart + trimmedLine.Length);
                                lineIndex = lineStart + trimmedLine.Length;
                            }
                        }
                    }
                    break;
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