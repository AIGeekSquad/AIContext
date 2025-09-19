using System;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Represents a segment of text with its position in the original text.
    /// </summary>
    public class TextSegment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextSegment"/> class.
        /// </summary>
        /// <param name="text">The text content of the segment.</param>
        /// <param name="startIndex">The starting index of the segment in the original text.</param>
        /// <param name="endIndex">The ending index of the segment in the original text.</param>
        public TextSegment(string text, int startIndex, int endIndex)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        /// <summary>
        /// Gets the text content of the segment.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the starting index of the segment in the original text.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the ending index of the segment in the original text.
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// Gets the length of the segment text.
        /// </summary>
        public int Length => Text.Length;

        /// <summary>
        /// Returns a string representation of the text segment.
        /// </summary>
        /// <returns>A string that represents the current text segment.</returns>
        public override string ToString()
        {
            return $"TextSegment[{StartIndex}-{EndIndex}]: {Text.Substring(0, Math.Min(Text.Length, 50))}{(Text.Length > 50 ? "..." : "")}";
        }
    }
}