using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Represents a group of sentences with their combined text and optional embedding.
    /// Used internally for semantic chunking to maintain context around sentence boundaries.
    /// </summary>
    internal class SentenceGroup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SentenceGroup"/> class.
        /// </summary>
        /// <param name="sentences">The sentences that make up this group.</param>
        /// <param name="startIndex">The starting index in the original text.</param>
        /// <param name="endIndex">The ending index in the original text.</param>
        public SentenceGroup(IEnumerable<string> sentences, int startIndex, int endIndex)
        {
            Sentences = sentences?.ToList() ?? throw new ArgumentNullException(nameof(sentences));
            StartIndex = startIndex;
            EndIndex = endIndex;
            CombinedText = string.Join(" ", Sentences);
        }

        /// <summary>
        /// Gets the individual sentences in this group.
        /// </summary>
        public IReadOnlyList<string> Sentences { get; }

        /// <summary>
        /// Gets the combined text of all sentences in the group.
        /// </summary>
        public string CombinedText { get; }

        /// <summary>
        /// Gets the starting index of this sentence group in the original text.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// Gets the ending index of this sentence group in the original text.
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// Gets or sets the embedding vector for this sentence group.
        /// This is lazily computed and cached when needed.
        /// </summary>
        public Vector<double>? Embedding { get; set; }

        /// <summary>
        /// Gets a value indicating whether this sentence group has a computed embedding.
        /// </summary>
        public bool HasEmbedding => Embedding != null;

        /// <summary>
        /// Gets the number of sentences in this group.
        /// </summary>
        public int SentenceCount => Sentences.Count;

        /// <summary>
        /// Returns a string representation of the sentence group.
        /// </summary>
        /// <returns>A string that represents the current sentence group.</returns>
        public override string ToString()
        {
            var preview = CombinedText.Length > 100
                ? CombinedText.Substring(0, 100) + "..."
                : CombinedText;
            return $"SentenceGroup[{StartIndex}-{EndIndex}, {SentenceCount} sentences]: {preview}";
        }
    }
}