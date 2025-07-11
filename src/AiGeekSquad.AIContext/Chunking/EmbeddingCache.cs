using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace AiGeekSquad.AIContext.Chunking
{
    /// <summary>
    /// Provides caching functionality for embeddings to improve performance by avoiding redundant computations.
    /// </summary>
    internal class EmbeddingCache
    {
        private readonly ConcurrentDictionary<string, Vector<double>> _cache;
        private readonly int _maxCacheSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingCache"/> class.
        /// </summary>
        /// <param name="maxCacheSize">The maximum number of embeddings to cache.</param>
        public EmbeddingCache(int maxCacheSize = 1000)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new ConcurrentDictionary<string, Vector<double>>();
        }

        /// <summary>
        /// Gets the number of cached embeddings.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxCacheSize => _maxCacheSize;

        /// <summary>
        /// Attempts to get a cached embedding for the specified text.
        /// </summary>
        /// <param name="text">The text to get the embedding for.</param>
        /// <param name="embedding">When this method returns, contains the cached embedding if found; otherwise, null.</param>
        /// <returns>true if the embedding was found in the cache; otherwise, false.</returns>
        public bool TryGetEmbedding(string text, out Vector<double>? embedding)
        {
            if (string.IsNullOrEmpty(text))
            {
                embedding = null;
                return false;
            }

            var key = ComputeTextHash(text);
            return _cache.TryGetValue(key, out embedding);
        }

        /// <summary>
        /// Stores an embedding in the cache for the specified text.
        /// </summary>
        /// <param name="text">The text associated with the embedding.</param>
        /// <param name="embedding">The embedding to cache.</param>
        public void StoreEmbedding(string text, Vector<double> embedding)
        {
            if (string.IsNullOrEmpty(text) || embedding == null)
                return;

            var key = ComputeTextHash(text);

            // If cache is at capacity, remove some entries to make room
            if (_cache.Count >= _maxCacheSize)
            {
                ClearOldestEntries();
            }

            _cache.TryAdd(key, embedding);
        }

        /// <summary>
        /// Clears all cached embeddings.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Computes a hash of the input text for use as a cache key.
        /// </summary>
        /// <param name="text">The text to hash.</param>
        /// <returns>A hash string representing the text.</returns>
        private static string ComputeTextHash(string text)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Removes approximately 25% of the oldest entries to make room for new ones.
        /// This is a simple eviction strategy - in a production system, you might want LRU or other policies.
        /// </summary>
        private void ClearOldestEntries()
        {
            var entriesToRemove = Math.Max(1, _maxCacheSize / 4);
            var removedCount = 0;

            // Simple eviction - remove first entries found (not truly LRU but sufficient for this implementation)
            foreach (var key in _cache.Keys)
            {
                if (removedCount >= entriesToRemove)
                    break;

                if (_cache.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }
        }
    }
}