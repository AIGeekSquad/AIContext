using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceTextSplitter"/> class.
        /// </summary>
        /// <param name="pattern">Optional custom regex pattern for sentence splitting. If null, uses default pattern
        /// that handles common English titles and abbreviations (Mr., Mrs., Ms., Dr., Prof., Sr., Jr.).</param>
        public SentenceTextSplitter(string? pattern = null)
        {
            // Default pattern: Split on sentence endings followed by whitespace and capital letter
            // Uses negative lookbehind assertions to avoid splitting on common English abbreviations:
            // - Mr. (Mister)
            // - Mrs. (Missus)
            // - Ms. (Miss/Ms.)
            // - Dr. (Doctor)
            // - Prof. (Professor)
            // - Sr. (Senior)
            // - Jr. (Junior)
            // The pattern avoids splitting after these abbreviations by checking for specific patterns
            var defaultPattern = @"(?<!Mr\.)(?<!Mrs\.)(?<!Ms\.)(?<!Dr\.)(?<!Prof\.)(?<!Sr\.)(?<!Jr\.)(?<=[.!?])\s+(?=[A-Z])";
            _sentencePattern = new Regex(pattern ?? defaultPattern, RegexOptions.Compiled);
        }

        /// <summary>
        /// Asynchronously splits the specified text into sentence segments.
        /// </summary>
        /// <param name="text">The text to split into sentences.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of text segments representing sentences.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public async IAsyncEnumerable<TextSegment> SplitAsync(
            string text,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

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

                // Yield control to allow cancellation and avoid blocking
                await System.Threading.Tasks.Task.Yield();
            }

            // Fallback: if no sentence boundaries found, treat entire text as one segment
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
    }
}