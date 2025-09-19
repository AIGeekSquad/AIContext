using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MathNet.Numerics.LinearAlgebra;
using AiGeekSquad.AIContext.Chunking;
using AiGeekSquad.AIContext.Ranking;

namespace AiGeekSquad.AIContext.Examples
{
    /// <summary>
    /// Enterprise-grade RAG service demonstrating production-ready patterns with MMR integration.
    /// 
    /// This implementation showcases:
    /// - Intelligent caching with TTL for performance optimization
    /// - Two-stage retrieval pattern (retrieve candidates, then apply MMR)
    /// - Adaptive lambda selection based on query characteristics
    /// - Comprehensive error handling and logging
    /// - Dependency injection ready architecture
    /// - Request tracing for observability
    /// - Domain-specific behavior configuration
    /// </summary>
    public class EnterpriseRAGService
    {
        private readonly IEmbeddingGenerator _embeddingGenerator;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly ILanguageModel _languageModel;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EnterpriseRAGService> _logger;
        private readonly RAGServiceOptions _options;

        public EnterpriseRAGService(
            IEmbeddingGenerator embeddingGenerator,
            IVectorDatabase vectorDatabase,
            ILanguageModel languageModel,
            IMemoryCache cache,
            ILogger<EnterpriseRAGService> logger,
            IOptions<RAGServiceOptions> options)
        {
            _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
            _vectorDatabase = vectorDatabase ?? throw new ArgumentNullException(nameof(vectorDatabase));
            _languageModel = languageModel ?? throw new ArgumentNullException(nameof(languageModel));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Processes a user question using enterprise RAG pipeline with MMR optimization.
        /// </summary>
        /// <param name="question">The user's question</param>
        /// <param name="userId">User identifier for personalization and tracking</param>
        /// <param name="domain">Domain context for adaptive behavior (e.g., "support", "research", "legal")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>RAG response with comprehensive metadata</returns>
        public async Task<RAGResponse> AskQuestionAsync(
            string question, 
            string userId = null, 
            string domain = "general",
            CancellationToken cancellationToken = default)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting RAG request {RequestId} for user {UserId} in domain {Domain}: {Question}", 
                requestId, userId ?? "anonymous", domain, question);

            try
            {
                // 1. CACHING LAYER - Check for cached responses
                var cacheKey = ComputeCacheKey(question, domain);
                if (_cache.TryGetValue(cacheKey, out RAGResponse cachedResponse))
                {
                    _logger.LogInformation("Cache hit for request {RequestId}", requestId);
                    cachedResponse.RequestId = requestId;
                    cachedResponse.FromCache = true;
                    return cachedResponse;
                }

                // 2. EMBEDDING GENERATION - Convert question to vector
                _logger.LogDebug("Generating embedding for request {RequestId}", requestId);
                var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(question, cancellationToken);

                // 3. CANDIDATE RETRIEVAL - Cast wide net initially
                _logger.LogDebug("Retrieving candidates for request {RequestId}", requestId);
                var candidates = await _vectorDatabase.SearchAsync(
                    queryEmbedding, 
                    limit: _options.CandidateRetrievalLimit, 
                    cancellationToken);

                if (!candidates.Any())
                {
                    _logger.LogWarning("No candidates found for request {RequestId}", requestId);
                    return CreateErrorResponse("No relevant information found", requestId);
                }

                // 4. MMR SELECTION - Balance relevance with diversity
                var lambda = GetOptimalLambda(question, domain);
                _logger.LogDebug("Applying MMR with lambda {Lambda} for request {RequestId}", lambda, requestId);
                
                var selectedDocs = MaximumMarginalRelevance.ComputeMMR(
                    vectors: candidates.Select(c => c.Embedding).ToList(),
                    query: queryEmbedding,
                    lambda: lambda,
                    topK: _options.FinalSelectionLimit
                );

                var selectedCandidates = selectedDocs.Select(doc => candidates[doc.index]).ToList();

                // 5. RESPONSE GENERATION - Create final answer
                _logger.LogDebug("Generating response for request {RequestId} with {DocumentCount} documents", 
                    requestId, selectedCandidates.Count);
                
                var context = BuildContext(selectedCandidates);
                var generatedResponse = await _languageModel.GenerateResponseAsync(question, context, cancellationToken);

                var response = new RAGResponse
                {
                    RequestId = requestId,
                    Answer = generatedResponse,
                    Success = true,
                    SourceDocuments = selectedCandidates.Select(c => new SourceDocument
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Content = c.Content,
                        RelevanceScore = CalculateRelevanceScore(queryEmbedding, c.Embedding),
                        Source = c.Source
                    }).ToList(),
                    Metadata = new ResponseMetadata
                    {
                        ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds,
                        CandidatesRetrieved = candidates.Count,
                        DocumentsSelected = selectedCandidates.Count,
                        Lambda = lambda,
                        Domain = domain,
                        FromCache = false
                    }
                };

                // 6. CACHING & LOGGING - Store for future use
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CacheTTL,
                    Priority = CacheItemPriority.Normal
                };
                _cache.Set(cacheKey, response, cacheOptions);

                _logger.LogInformation("Completed RAG request {RequestId} in {ProcessingTime}ms with {DocumentCount} documents", 
                    requestId, response.Metadata.ProcessingTimeMs, selectedCandidates.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG request {RequestId}: {Error}", requestId, ex.Message);
                return CreateErrorResponse($"Processing failed: {ex.Message}", requestId);
            }
        }

        /// <summary>
        /// Adaptive lambda selection based on query characteristics and domain context.
        /// This method demonstrates intelligent parameter tuning for different use cases.
        /// </summary>
        /// <param name="question">The user's question</param>
        /// <param name="domain">Domain context</param>
        /// <returns>Optimal lambda value for the query</returns>
        private double GetOptimalLambda(string question, string domain)
        {
            var questionLower = question.ToLowerInvariant();

            // Query-based lambda selection
            if (questionLower.Contains("how to") || questionLower.Contains("steps") || questionLower.Contains("procedure"))
            {
                _logger.LogDebug("Detected procedural query, using high relevance lambda");
                return 0.8; // Precision needed for step-by-step instructions
            }

            if (questionLower.Contains("compare") || questionLower.Contains("different") || questionLower.Contains("alternatives"))
            {
                _logger.LogDebug("Detected comparative query, using balanced lambda");
                return 0.5; // Diversity needed for comprehensive comparison
            }

            if (questionLower.Contains("overview") || questionLower.Contains("summary") || questionLower.Contains("explain"))
            {
                _logger.LogDebug("Detected exploratory query, using diversity-focused lambda");
                return 0.6; // Moderate diversity for comprehensive coverage
            }

            // Domain-based defaults
            var domainLambda = domain.ToLowerInvariant() switch
            {
                "support" => 0.8,      // Customer support needs precise answers
                "research" => 0.6,     // Research benefits from diverse perspectives
                "legal" => 0.9,        // Legal queries need high precision
                "medical" => 0.85,     // Medical information requires accuracy
                "technical" => 0.75,   // Technical docs need relevant but comprehensive info
                "creative" => 0.4,     // Creative tasks benefit from diversity
                _ => 0.7               // Balanced default
            };

            _logger.LogDebug("Using domain-based lambda {Lambda} for domain {Domain}", domainLambda, domain);
            return domainLambda;
        }

        /// <summary>
        /// Computes a cache key for the given question and domain.
        /// </summary>
        private static string ComputeCacheKey(string question, string domain)
        {
            var combined = $"{domain}:{question}";
            return $"rag:{combined.GetHashCode():X}";
        }

        /// <summary>
        /// Builds context string from selected documents for the language model.
        /// </summary>
        private static string BuildContext(List<DocumentCandidate> documents)
        {
            var contextParts = documents.Select((doc, index) => 
                $"[Document {index + 1}: {doc.Title}]\n{doc.Content}\n");
            
            return string.Join("\n---\n", contextParts);
        }

        /// <summary>
        /// Calculates relevance score between query and document embeddings.
        /// </summary>
        private static double CalculateRelevanceScore(Vector<double> queryEmbedding, Vector<double> docEmbedding)
        {
            return 1.0 - MathNet.Numerics.Distance.Cosine(queryEmbedding.ToArray(), docEmbedding.ToArray());
        }

        /// <summary>
        /// Creates an error response with proper structure.
        /// </summary>
        private static RAGResponse CreateErrorResponse(string message, string requestId)
        {
            return new RAGResponse
            {
                RequestId = requestId,
                Answer = $"I apologize, but I encountered an issue processing your request. {message}",
                Success = false,
                SourceDocuments = new List<SourceDocument>(),
                Metadata = new ResponseMetadata
                {
                    ProcessingTimeMs = 0,
                    CandidatesRetrieved = 0,
                    DocumentsSelected = 0,
                    Lambda = 0,
                    Domain = "error",
                    FromCache = false
                }
            };
        }
    }

    #region Supporting Types and Interfaces

    /// <summary>
    /// Configuration options for the RAG service.
    /// </summary>
    public class RAGServiceOptions
    {
        /// <summary>
        /// Number of candidates to retrieve in the first stage (default: 25)
        /// </summary>
        public int CandidateRetrievalLimit { get; set; } = 25;

        /// <summary>
        /// Number of documents to select with MMR for final context (default: 5)
        /// </summary>
        public int FinalSelectionLimit { get; set; } = 5;

        /// <summary>
        /// Cache time-to-live for responses (default: 15 minutes)
        /// </summary>
        public TimeSpan CacheTTL { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Response from the RAG service with comprehensive metadata.
    /// </summary>
    public class RAGResponse
    {
        public string RequestId { get; set; }
        public string Answer { get; set; }
        public bool Success { get; set; }
        public List<SourceDocument> SourceDocuments { get; set; } = new();
        public ResponseMetadata Metadata { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// Metadata about the RAG processing pipeline.
    /// </summary>
    public class ResponseMetadata
    {
        public int ProcessingTimeMs { get; set; }
        public int CandidatesRetrieved { get; set; }
        public int DocumentsSelected { get; set; }
        public double Lambda { get; set; }
        public string Domain { get; set; }
        public bool FromCache { get; set; }
    }

    /// <summary>
    /// Source document information included in responses.
    /// </summary>
    public class SourceDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public double RelevanceScore { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// Document candidate from vector database search.
    /// </summary>
    public class DocumentCandidate
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Vector<double> Embedding { get; set; }
        public string Source { get; set; }
        public double Score { get; set; }
    }

    /// <summary>
    /// Interface for vector database operations.
    /// </summary>
    public interface IVectorDatabase
    {
        Task<List<DocumentCandidate>> SearchAsync(
            Vector<double> queryEmbedding, 
            int limit, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for language model operations.
    /// </summary>
    public interface ILanguageModel
    {
        Task<string> GenerateResponseAsync(
            string question, 
            string context, 
            CancellationToken cancellationToken = default);
    }

    #endregion

    #region Example Usage and Dependency Injection Setup

    /// <summary>
    /// Example demonstrating how to set up and use the Enterprise RAG Service.
    /// This shows the complete integration including dependency injection configuration.
    /// </summary>
    public static class EnterpriseRAGServiceExample
    {
        /// <summary>
        /// Example of configuring the service with dependency injection.
        /// Add this to your Program.cs or Startup.cs
        /// </summary>
        public static void ConfigureServices(IServiceCollection services)
        {
            // Configure RAG service options
            services.Configure<RAGServiceOptions>(options =>
            {
                options.CandidateRetrievalLimit = 25;
                options.FinalSelectionLimit = 5;
                options.CacheTTL = TimeSpan.FromMinutes(15);
            });

            // Register dependencies
            services.AddMemoryCache();
            services.AddLogging();
            
            // Register your implementations
            services.AddScoped<IEmbeddingGenerator, YourEmbeddingGenerator>();
            services.AddScoped<IVectorDatabase, YourVectorDatabase>();
            services.AddScoped<ILanguageModel, YourLanguageModel>();
            
            // Register the RAG service
            services.AddScoped<EnterpriseRAGService>();
        }

        /// <summary>
        /// Example usage of the Enterprise RAG Service.
        /// </summary>
        public static async Task<RAGResponse> ExampleUsage(EnterpriseRAGService ragService)
        {
            // Customer support scenario
            var supportResponse = await ragService.AskQuestionAsync(
                question: "How do I reset my password?",
                userId: "user123",
                domain: "support"
            );

            Console.WriteLine($"Support Response (λ={supportResponse.Metadata.Lambda}):");
            Console.WriteLine($"Answer: {supportResponse.Answer}");
            Console.WriteLine($"Sources: {supportResponse.SourceDocuments.Count} documents");
            Console.WriteLine($"Processing time: {supportResponse.Metadata.ProcessingTimeMs}ms");
            Console.WriteLine();

            // Research scenario
            var researchResponse = await ragService.AskQuestionAsync(
                question: "Compare different machine learning optimization techniques",
                userId: "researcher456",
                domain: "research"
            );

            Console.WriteLine($"Research Response (λ={researchResponse.Metadata.Lambda}):");
            Console.WriteLine($"Answer: {researchResponse.Answer}");
            Console.WriteLine($"Sources: {researchResponse.SourceDocuments.Count} documents");
            Console.WriteLine($"Processing time: {researchResponse.Metadata.ProcessingTimeMs}ms");

            return supportResponse;
        }

        /// <summary>
        /// Example of monitoring and observability integration.
        /// </summary>
        public static void LogResponseMetrics(RAGResponse response, ILogger logger)
        {
            logger.LogInformation("RAG Metrics - Request: {RequestId}, " +
                "ProcessingTime: {ProcessingTime}ms, " +
                "CandidatesRetrieved: {CandidatesRetrieved}, " +
                "DocumentsSelected: {DocumentsSelected}, " +
                "Lambda: {Lambda}, " +
                "Domain: {Domain}, " +
                "FromCache: {FromCache}",
                response.RequestId,
                response.Metadata.ProcessingTimeMs,
                response.Metadata.CandidatesRetrieved,
                response.Metadata.DocumentsSelected,
                response.Metadata.Lambda,
                response.Metadata.Domain,
                response.FromCache);
        }
    }

    #endregion

    #region Mock Implementations for Testing

    /// <summary>
    /// Mock embedding generator for testing and demonstration purposes.
    /// Replace with your actual embedding service (OpenAI, Azure OpenAI, etc.)
    /// </summary>
    public class MockEmbeddingGenerator : IEmbeddingGenerator
    {
        private readonly RandomNumberGenerator _randomGenerator = RandomNumberGenerator.Create();

        public Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            // Generate a mock 384-dimensional embedding (typical for sentence transformers)
            var embedding = Vector<double>.Build.Dense(384, i => GetNextDouble() - 0.5);
            return Task.FromResult(embedding.Normalize(2)); // L2 normalize
        }

        private double GetNextDouble()
        {
            var bytes = new byte[8];
            _randomGenerator.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0) / (double)ulong.MaxValue;
        }

        public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
            IEnumerable<string> texts, 
            CancellationToken cancellationToken = default)
        {
            foreach (var text in texts)
            {
                yield return await GenerateEmbeddingAsync(text, cancellationToken);
            }
        }

        public void Dispose()
        {
            _randomGenerator?.Dispose();
        }
    }

    /// <summary>
    /// Mock vector database for testing and demonstration purposes.
    /// Replace with your actual vector database (Pinecone, Weaviate, Qdrant, etc.)
    /// </summary>
    public class MockVectorDatabase : IVectorDatabase
    {
        private readonly List<DocumentCandidate> _documents;
        private readonly RandomNumberGenerator _randomGenerator = RandomNumberGenerator.Create();

        public MockVectorDatabase()
        {
            _documents = GenerateMockDocuments();
        }

        public Task<List<DocumentCandidate>> SearchAsync(
            Vector<double> queryEmbedding, 
            int limit, 
            CancellationToken cancellationToken = default)
        {
            // Simulate vector search by computing cosine similarity
            var results = _documents
                .Select(doc => new DocumentCandidate
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    Content = doc.Content,
                    Embedding = doc.Embedding,
                    Source = doc.Source,
                    Score = 1.0 - MathNet.Numerics.Distance.Cosine(
                        queryEmbedding.ToArray(), 
                        doc.Embedding.ToArray())
                })
                .OrderByDescending(d => d.Score)
                .Take(limit)
                .ToList();

            return Task.FromResult(results);
        }

        private List<DocumentCandidate> GenerateMockDocuments()
        {
            var documents = new List<DocumentCandidate>();
            var categories = new[] { "technical", "support", "legal", "medical", "general" };
            
            for (int i = 0; i < 100; i++)
            {
                var category = categories[i % categories.Length];
                documents.Add(new DocumentCandidate
                {
                    Id = $"doc_{i:D3}",
                    Title = $"Document {i}: {category} information",
                    Content = $"This is mock content for document {i} in the {category} category. " +
                             $"It contains relevant information that would be useful for answering questions " +
                             $"related to {category} topics.",
                    Embedding = Vector<double>.Build.Dense(384, j => GetNextDouble() - 0.5).Normalize(2),
                    Source = $"source_{category}.pdf"
                });
            }

            return documents;
        }

        private double GetNextDouble()
        {
            var bytes = new byte[8];
            _randomGenerator.GetBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0) / (double)ulong.MaxValue;
        }

        public void Dispose()
        {
            _randomGenerator?.Dispose();
        }
    }

    /// <summary>
    /// Mock language model for testing and demonstration purposes.
    /// Replace with your actual language model (OpenAI GPT, Azure OpenAI, etc.)
    /// </summary>
    public class MockLanguageModel : ILanguageModel
    {
        public Task<string> GenerateResponseAsync(
            string question, 
            string context, 
            CancellationToken cancellationToken = default)
        {
            var response = $"Based on the provided context, here's my response to '{question}': " +
                          $"This is a mock response that would normally be generated by a language model " +
                          $"using the retrieved context. The context contained {context.Split('\n').Length} lines " +
                          $"of relevant information that would be used to formulate a comprehensive answer.";

            return Task.FromResult(response);
        }
    }

    #endregion
}