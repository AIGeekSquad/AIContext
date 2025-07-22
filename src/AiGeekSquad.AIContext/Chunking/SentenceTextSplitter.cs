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
        /// Comprehensive markdown-aware splitting that handles all edge cases.
        /// </summary>
        private IEnumerable<TextSegment> MarkdownAwareSplit(string text, CancellationToken cancellationToken)
        {
            // Step 1: Detect malformed markdown and handle it specially
            if (IsMalformedMarkdown(text))
            {
                return HandleMalformedMarkdown(text);
            }

            // Step 2: Preprocess mixed content patterns
            var processedText = PreprocessMixedContent(text);
            
            // Step 3: Parse with Markdig
            var pipeline = new MarkdownPipelineBuilder().Build();
            var document = Markdown.Parse(processedText, pipeline);
            
            // Step 4: Extract segments with proper handling for each block type
            var segments = new List<TextSegment>();
            
            foreach (var block in document)
            {
                cancellationToken.ThrowIfCancellationRequested();
                segments.AddRange(ExtractSegmentsFromBlock(block, processedText, text));
            }
            
            // Step 5: Handle any remaining unprocessed text
            var processedSegments = HandleUnprocessedText(segments, text);
            
            return processedSegments.OrderBy(s => s.StartIndex);
        }

        /// <summary>
        /// Detects if markdown is malformed (lacks proper spacing/formatting).
        /// </summary>
        private bool IsMalformedMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var lines = text.Split('\n');
            var result = lines.Any(line =>
            {
                var trimmed = line.Trim();
                return (trimmed.StartsWith("-") && !trimmed.StartsWith("- ")) ||
                       (trimmed.StartsWith("*") && !trimmed.StartsWith("* ")) ||
                       (trimmed.StartsWith("+") && !trimmed.StartsWith("+ ")) ||
                       (trimmed.StartsWith("#") && !trimmed.StartsWith("# ")) ||
                       Regex.IsMatch(trimmed, @"^\d+\.[^\s]");
            });
            
            // DEBUG: Log malformed markdown detection
            System.Diagnostics.Debug.WriteLine($"[DEBUG] IsMalformedMarkdown result: {result} for text: {text.Replace('\n', '\\').Substring(0, Math.Min(50, text.Length))}...");
            
            return result;
        }

        /// <summary>
        /// Handles malformed markdown by splitting line by line, but preserves fenced code blocks as atomic units.
        /// </summary>
        private IEnumerable<TextSegment> HandleMalformedMarkdown(string text)
        {
            // DEBUG: Log entry into HandleMalformedMarkdown
            System.Diagnostics.Debug.WriteLine($"[DEBUG] HandleMalformedMarkdown called with text: {text.Replace('\n', '\\').Substring(0, Math.Min(100, text.Length))}...");
            
            var lines = text.Split('\n');
            var currentIndex = 0;
            var segmentCount = 0;
            
            // First pass: identify fenced code block ranges
            var fencedRanges = new List<(int start, int end)>();
            var insideFencedBlock = false;
            var fencedBlockStart = -1;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.Trim() == "```")
                {
                    if (!insideFencedBlock)
                    {
                        insideFencedBlock = true;
                        fencedBlockStart = i;
                    }
                    else
                    {
                        insideFencedBlock = false;
                        if (fencedBlockStart >= 0)
                        {
                            fencedRanges.Add((fencedBlockStart, i));
                        }
                    }
                }
            }
            
            // Second pass: process lines, merging fenced blocks
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.TrimEnd('\r');
                
                // Check if this line is part of a fenced block
                var fencedRange = fencedRanges.FirstOrDefault(r => i >= r.start && i <= r.end);
                if (fencedRange != default && i == fencedRange.start)
                {
                    // Start of fenced block - collect all lines in the range
                    var fencedLines = new List<string>();
                    for (int j = fencedRange.start; j <= fencedRange.end; j++)
                    {
                        fencedLines.Add(lines[j]);
                    }
                    
                    var fencedContent = string.Join("\n", fencedLines);
                    var startIndex = text.IndexOf(fencedContent, currentIndex, StringComparison.Ordinal);
                    if (startIndex >= 0)
                    {
                        segmentCount++;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] HandleMalformedMarkdown fenced block segment #{segmentCount}: '{fencedContent.Replace('\n', '\\')}'");
                        yield return new TextSegment(fencedContent, startIndex, startIndex + fencedContent.Length);
                        currentIndex = startIndex + fencedContent.Length + 1;
                    }
                    
                    // Skip to end of fenced block
                    i = fencedRange.end;
                }
                else if (fencedRange == default)
                {
                    // Regular line processing (not part of fenced block)
                    if (string.IsNullOrWhiteSpace(trimmedLine))
                    {
                        currentIndex += line.Length + 1; // +1 for newline
                        continue;
                    }

                    var startIndex = text.IndexOf(trimmedLine, currentIndex, StringComparison.Ordinal);
                    if (startIndex >= 0)
                    {
                        segmentCount++;
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] HandleMalformedMarkdown regular segment #{segmentCount}: '{trimmedLine}'");
                        yield return new TextSegment(trimmedLine, startIndex, startIndex + trimmedLine.Length);
                        currentIndex = startIndex + trimmedLine.Length + 1; // +1 for newline
                    }
                    else
                    {
                        currentIndex += line.Length + 1;
                    }
                }
                // Skip lines that are part of fenced blocks but not the start
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] HandleMalformedMarkdown produced {segmentCount} segments");
        }

        /// <summary>
        /// Preprocesses text to handle mixed content patterns that Markdig doesn't parse correctly.
        /// </summary>
        private string PreprocessMixedContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var result = text;
            
            // Pattern 1: "sentence. - list item" -> "sentence.\n- list item"
            result = Regex.Replace(result, @"([.!?])\s+([-*+])\s", "$1\n$2 ");
            
            // Pattern 2: "sentence.\nAnother sentence" after list items
            result = Regex.Replace(result, @"([-*+]\s+[^\n]*)\n([A-Z][^-*+\n]*[.!?])", "$1\n\n$2");
            
            return result;
        }

        /// <summary>
        /// Extracts segments from a markdown block with comprehensive handling.
        /// </summary>
        private IEnumerable<TextSegment> ExtractSegmentsFromBlock(Block block, string processedText, string originalText)
        {
            var blockStart = Math.Max(0, Math.Min(block.Span.Start, processedText.Length));
            var blockEnd = Math.Max(blockStart, Math.Min(block.Span.End + 1, processedText.Length));
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Block type: {block.GetType().Name}, Span: {block.Span.Start}-{block.Span.End}");
            
            if (blockStart >= blockEnd)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Invalid span, skipping block");
                yield break;
            }

            switch (block)
            {
                case ListBlock listBlock:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing ListBlock");
                    foreach (var segment in ExtractListSegments(listBlock, processedText, originalText))
                        yield return segment;
                    break;

                case HeadingBlock headingBlock:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing HeadingBlock");
                    foreach (var segment in ExtractHeadingSegments(headingBlock, processedText, originalText))
                        yield return segment;
                    break;

                case CodeBlock codeBlock:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing CodeBlock ({codeBlock.GetType().Name})");
                    foreach (var segment in ExtractCodeBlockSegments(codeBlock, processedText, originalText))
                        yield return segment;
                    break;

                case ParagraphBlock paragraphBlock:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing ParagraphBlock");
                    foreach (var segment in ExtractParagraphSegments(paragraphBlock, processedText, originalText))
                        yield return segment;
                    break;

                case QuoteBlock quoteBlock:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing QuoteBlock");
                    foreach (var segment in ExtractQuoteSegments(quoteBlock, processedText, originalText))
                        yield return segment;
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractSegmentsFromBlock - Processing unknown block type: {block.GetType().Name}");
                    // Fallback for other block types
                    foreach (var segment in ExtractFallbackSegments(block, processedText, originalText))
                        yield return segment;
                    break;
            }
        }

        /// <summary>
        /// Extracts segments from list blocks, handling nested lists properly.
        /// </summary>
        private IEnumerable<TextSegment> ExtractListSegments(ListBlock listBlock, string processedText, string originalText)
        {
            foreach (var listItemBlock in listBlock.OfType<ListItemBlock>())
            {
                var itemStart = Math.Max(0, Math.Min(listItemBlock.Span.Start, processedText.Length));
                var itemEnd = Math.Max(itemStart, Math.Min(listItemBlock.Span.End + 1, processedText.Length));

                if (itemStart >= itemEnd) continue;

                var itemText = processedText.Substring(itemStart, itemEnd - itemStart);
                var lines = itemText.Split('\n');

                foreach (var line in lines)
                {
                    var trimmedLine = line.TrimEnd('\r');
                    if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                    // For list items, preserve indentation when needed
                    var originalSegment = FindInOriginalTextWithIndentation(trimmedLine, originalText, itemStart);
                    if (originalSegment != null)
                        yield return originalSegment;
                }
            }
        }

        /// <summary>
        /// Extracts segments from heading blocks.
        /// </summary>
        private IEnumerable<TextSegment> ExtractHeadingSegments(HeadingBlock headingBlock, string processedText, string originalText)
        {
            var blockStart = Math.Max(0, Math.Min(headingBlock.Span.Start, processedText.Length));
            var blockEnd = Math.Max(blockStart, Math.Min(headingBlock.Span.End + 1, processedText.Length));

            if (blockStart >= blockEnd) yield break;

            var headingText = processedText.Substring(blockStart, blockEnd - blockStart).Trim();
            
            // Handle empty headings - preserve trailing space
            if (string.IsNullOrEmpty(headingText.Replace("#", "").Trim()))
            {
                var originalHeading = processedText.Substring(blockStart, blockEnd - blockStart);
                var originalSegment = FindInOriginalTextPreservingWhitespace(originalHeading, originalText, blockStart);
                if (originalSegment != null)
                    yield return originalSegment;
            }
            else
            {
                var originalSegment = FindInOriginalText(headingText, originalText, blockStart);
                if (originalSegment != null)
                    yield return originalSegment;
            }
        }

        /// <summary>
        /// Extracts segments from code blocks, preserving indentation and whitespace.
        /// </summary>
        private IEnumerable<TextSegment> ExtractCodeBlockSegments(CodeBlock codeBlock, string processedText, string originalText)
        {
            var blockStart = Math.Max(0, Math.Min(codeBlock.Span.Start, processedText.Length));
            var blockEnd = Math.Max(blockStart, Math.Min(codeBlock.Span.End + 1, processedText.Length));

            System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments called - Type: {codeBlock.GetType().Name}, blockStart: {blockStart}, blockEnd: {blockEnd}");

            if (blockStart >= blockEnd)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments - Invalid range, yielding nothing");
                yield break;
            }

            if (codeBlock is FencedCodeBlock)
            {
                // Fenced code block - preserve as-is including multiline and empty content
                var codeText = processedText.Substring(blockStart, blockEnd - blockStart);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments - FencedCodeBlock content: '{codeText.Replace('\n', '\\').Replace('\r', 'r')}'");
                
                // For empty fenced code blocks, preserve exact formatting
                var originalSegment = FindInOriginalTextPreservingWhitespace(codeText, originalText, blockStart);
                if (originalSegment != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments - Created segment: '{originalSegment.Text.Replace('\n', '\\').Replace('\r', 'r')}'");
                    yield return originalSegment;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments - Failed to find original segment");
                }
            }
            else
            {
                // Indented code block - preserve indentation
                var codeText = processedText.Substring(blockStart, blockEnd - blockStart);
                var lines = codeText.Split('\n');

                System.Diagnostics.Debug.WriteLine($"[DEBUG] ExtractCodeBlockSegments - IndentedCodeBlock with {lines.Length} lines");

                foreach (var line in lines)
                {
                    var trimmedLine = line.TrimEnd('\r');
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    // For indented code blocks, preserve the original indentation
                    var originalSegment = FindInOriginalTextWithIndentation(trimmedLine, originalText, blockStart);
                    if (originalSegment != null)
                        yield return originalSegment;
                }
            }
        }

        /// <summary>
        /// Extracts segments from paragraph blocks, handling inline elements properly.
        /// </summary>
        private IEnumerable<TextSegment> ExtractParagraphSegments(ParagraphBlock paragraphBlock, string processedText, string originalText)
        {
            var blockStart = Math.Max(0, Math.Min(paragraphBlock.Span.Start, processedText.Length));
            var blockEnd = Math.Max(blockStart, Math.Min(paragraphBlock.Span.End + 1, processedText.Length));

            if (blockStart >= blockEnd) yield break;

            var paraText = processedText.Substring(blockStart, blockEnd - blockStart);
            var inlines = paragraphBlock.Inline?.ToList() ?? new List<Inline>();

            // Check if paragraph contains inline code
            var hasInlineCode = inlines.Any(i => i is CodeInline);
            
            if (hasInlineCode)
            {
                // Keep paragraphs with inline code as atomic segments
                var originalSegment = FindInOriginalText(paraText.Trim(), originalText, blockStart);
                if (originalSegment != null)
                    yield return originalSegment;
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
                            yield return originalSegment;
                    }
                }
            }
        }

        /// <summary>
        /// Splits paragraphs containing inline elements properly.
        /// </summary>
        private IEnumerable<TextSegment> SplitParagraphWithInlines(string paraText, string originalText, List<Inline> inlines, int blockStart)
        {
            // Use regex to split by inline code while preserving the inline code elements
            var pattern = @"(`[^`]+`)";
            var parts = Regex.Split(paraText, pattern);

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                var trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("`") && trimmedPart.EndsWith("`"))
                {
                    // This is inline code - keep as atomic segment
                    var originalSegment = FindInOriginalText(trimmedPart, originalText, blockStart);
                    if (originalSegment != null)
                        yield return originalSegment;
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
                            var originalSegment = FindInOriginalText(trimmedSentence, originalText, blockStart);
                            if (originalSegment != null)
                                yield return originalSegment;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extracts segments from quote blocks.
        /// </summary>
        private IEnumerable<TextSegment> ExtractQuoteSegments(QuoteBlock quoteBlock, string processedText, string originalText)
        {
            var blockStart = Math.Max(0, Math.Min(quoteBlock.Span.Start, processedText.Length));
            var blockEnd = Math.Max(blockStart, Math.Min(quoteBlock.Span.End + 1, processedText.Length));

            if (blockStart >= blockEnd) yield break;

            var quoteText = processedText.Substring(blockStart, blockEnd - blockStart);
            var lines = quoteText.Split('\n');

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
        /// Fallback segment extraction for unhandled block types.
        /// </summary>
        private IEnumerable<TextSegment> ExtractFallbackSegments(Block block, string processedText, string originalText)
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
        private TextSegment? FindInOriginalText(string text, string originalText, int searchStartHint = 0)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var trimmedText = text.Trim();
            if (string.IsNullOrEmpty(trimmedText)) return null;

            var startIndex = originalText.IndexOf(trimmedText, Math.Max(0, searchStartHint), StringComparison.Ordinal);
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
        private TextSegment? FindInOriginalTextWithIndentation(string text, string originalText, int searchStartHint = 0)
        {
            if (string.IsNullOrEmpty(text)) return null;

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
        private TextSegment? FindInOriginalTextPreservingWhitespace(string text, string originalText, int searchStartHint = 0)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var startIndex = originalText.IndexOf(text, Math.Max(0, searchStartHint), StringComparison.Ordinal);
            if (startIndex >= 0)
            {
                return new TextSegment(text, startIndex, startIndex + text.Length);
            }

            return null;
        }

        /// <summary>
        /// Handles any text that wasn't processed by markdown parsing.
        /// </summary>
        private List<TextSegment> HandleUnprocessedText(List<TextSegment> segments, string originalText)
        {
            // For now, just return the segments as-is
            // Could be extended to handle edge cases where some text is missed
            return segments;
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