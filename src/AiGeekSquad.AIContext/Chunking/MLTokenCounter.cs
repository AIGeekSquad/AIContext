using Microsoft.ML.Tokenizers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Implementation of <see cref="ITokenCounter"/> using Microsoft.ML.Tokenizers.
    /// Uses GPT-4 tokenizer for accurate token counting compatible with OpenAI models.
    /// </summary>
    public class MLTokenCounter : ITokenCounter
    {
        private readonly Tokenizer _tokenizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MLTokenCounter"/> class.
        /// </summary>
        /// <param name="tokenizer">The tokenizer to use for counting tokens. If null, uses GPT-4 tokenizer.</param>
        public MLTokenCounter(Tokenizer? tokenizer = null)
        {
            _tokenizer = tokenizer ?? GetDefaultTokenizer();
        }

        /// <summary>
        /// Gets the default GPT-4 tokenizer.
        /// </summary>
        /// <returns>A GPT-4 compatible tokenizer.</returns>
        private static Tokenizer GetDefaultTokenizer()
        {
            try
            {
                // Try to create GPT-4 tokenizer
                return TiktokenTokenizer.CreateForModel("gpt-4");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to create default tokenizer. Ensure Microsoft.ML.Tokenizers is properly installed.", ex);
            }
        }

        /// <summary>
        /// Counts the number of tokens in the specified text.
        /// </summary>
        /// <param name="text">The text to count tokens for.</param>
        /// <returns>The number of tokens in the text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public int CountTokens(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (string.IsNullOrEmpty(text))
                return 0;

            var encoded = _tokenizer.EncodeToIds(text);
            return encoded.Count;
        }

        /// <summary>
        /// Asynchronously counts the number of tokens in the specified text.
        /// </summary>
        /// <param name="text">The text to count tokens for.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of tokens in the text.</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
        public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Tokenization is typically fast enough to run synchronously
            // But we wrap it in Task.FromResult for the async interface
            var result = CountTokens(text);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Creates a new instance of <see cref="MLTokenCounter"/> with GPT-4 tokenizer.
        /// </summary>
        /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
        public static MLTokenCounter CreateGpt4()
        {
            return new MLTokenCounter(TiktokenTokenizer.CreateForModel("gpt-4"));
        }

        /// <summary>
        /// Creates a new instance of <see cref="MLTokenCounter"/> with GPT-3.5-turbo tokenizer.
        /// </summary>
        /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
        public static MLTokenCounter CreateGpt35Turbo()
        {
            return new MLTokenCounter(TiktokenTokenizer.CreateForModel("gpt-3.5-turbo"));
        }

        /// <summary>
        /// Creates a new instance of <see cref="MLTokenCounter"/> with a specific model tokenizer.
        /// </summary>
        /// <param name="modelName">The model name to create tokenizer for (e.g., "gpt-4", "gpt-3.5-turbo").</param>
        /// <returns>A new <see cref="MLTokenCounter"/> instance.</returns>
        /// <exception cref="ArgumentException">Thrown when model name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when tokenizer creation fails.</exception>
        public static MLTokenCounter CreateForModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));

            try
            {
                var tokenizer = TiktokenTokenizer.CreateForModel(modelName);
                return new MLTokenCounter(tokenizer);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create tokenizer for model '{modelName}'.", ex);
            }
        }

        /// <summary>
        /// Disposes the tokenizer resources.
        /// </summary>
        public void Dispose()
        {
            // Microsoft.ML.Tokenizers.Tokenizer doesn't implement IDisposable
            // No cleanup required for this implementation
        }
    }
}