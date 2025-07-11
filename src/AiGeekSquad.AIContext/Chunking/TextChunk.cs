using System.Collections.Generic;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Represents a chunk of text with associated metadata.
    /// </summary>
    public class TextChunk
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextChunk"/> class.
        /// </summary>
        /// <param name="text">The text content of the chunk.</param>
        /// <param name="startIndex">The starting index of the chunk in the original text.</param>
        /// <param name="endIndex">The ending index of the chunk in the original text.</param>
        /// <param name="metadata">Optional metadata associated with the chunk.</param>
        public TextChunk(string text, int startIndex, int endIndex, IDictionary<string, object>? metadata = null)
        {
            Text = text ?? throw new System.ArgumentNullException(nameof(text));
            StartIndex = startIndex;
            EndIndex = endIndex;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the text content of the chunk.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the starting index of the chunk in the original text.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the ending index of the chunk in the original text.
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// Gets the length of the chunk text.
        /// </summary>
        public int Length => Text.Length;

        /// <summary>
        /// Gets the metadata associated with the chunk.
        /// </summary>
        public IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Returns a string representation of the text chunk.
        /// </summary>
        /// <returns>A string that represents the current text chunk.</returns>
        public override string ToString()
        {
            return $"TextChunk[{StartIndex}-{EndIndex}]: {Text.Substring(0, System.Math.Min(Text.Length, 50))}{(Text.Length > 50 ? "..." : "")}";
        }
    }
}