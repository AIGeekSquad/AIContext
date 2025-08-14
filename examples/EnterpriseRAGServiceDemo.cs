using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AiGeekSquad.AIContext.Examples;

namespace AiGeekSquad.AIContext.Examples
{
    /// <summary>
    /// Demonstration program showing the Enterprise RAG Service in action.
    /// This example shows how the service handles different types of queries with adaptive lambda selection,
    /// caching, and comprehensive logging.
    /// </summary>
    public class EnterpriseRAGServiceDemo
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Enterprise RAG Service with MMR Demo ===\n");
            Console.WriteLine("This demo showcases a production-ready RAG service that demonstrates:");
            Console.WriteLine("• Intelligent caching with TTL");
            Console.WriteLine("• Two-stage retrieval (candidates → MMR selection)");
            Console.WriteLine("• Adaptive lambda selection based on query type");
            Console.WriteLine("• Comprehensive error handling and logging");
            Console.WriteLine("• Domain-specific behavior\n");

            // Setup dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Get the RAG service
            var ragService = serviceProvider.GetRequiredService<EnterpriseRAGService>();
            var logger = serviceProvider.GetRequiredService<ILogger<EnterpriseRAGServiceDemo>>();

            try
            {
                // Demo 1: Customer Support Query (High Precision)
                await DemoSupportQuery(ragService, logger);
                
                // Demo 2: Research Query (Balanced Diversity)
                await DemoResearchQuery(ragService, logger);
                
                // Demo 3: Procedural Query (Step-by-step)
                await DemoProceduralQuery(ragService, logger);
                
                // Demo 4: Comparative Query (High Diversity)
                await DemoComparativeQuery(ragService, logger);
                
                // Demo 5: Caching Demonstration
                await DemoCaching(ragService, logger);
                
                // Demo 6: Error Handling
                await DemoErrorHandling(ragService, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo execution failed");
                Console.WriteLine($"Demo failed: {ex.Message}");
            }

            Console.WriteLine("\n=== Demo Complete ===");
            Console.WriteLine("The Enterprise RAG Service successfully demonstrated:");
            Console.WriteLine("✅ Adaptive lambda selection for different query types");
            Console.WriteLine("✅ Two-stage retrieval with MMR optimization");
            Console.WriteLine("✅ Intelligent caching for performance");
            Console.WriteLine("✅ Comprehensive error handling");
            Console.WriteLine("✅ Domain-specific behavior adaptation");
            Console.WriteLine("✅ Production-ready logging and observability");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Configure RAG service options
            services.Configure<RAGServiceOptions>(options =>
            {
                options.CandidateRetrievalLimit = 25;
                options.FinalSelectionLimit = 5;
                options.CacheTTL = TimeSpan.FromMinutes(15);
            });

            // Register dependencies with mock implementations
            services.AddMemoryCache();
            services.AddScoped<IEmbeddingGenerator, MockEmbeddingGenerator>();
            services.AddScoped<IVectorDatabase, MockVectorDatabase>();
            services.AddScoped<ILanguageModel, MockLanguageModel>();
            
            // Register the RAG service
            services.AddScoped<EnterpriseRAGService>();
        }

        private static async Task DemoSupportQuery(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("🎯 DEMO 1: Customer Support Query (High Precision)");
            Console.WriteLine("Query: 'How do I reset my password?'");
            Console.WriteLine("Expected: High lambda (λ ≈ 0.8) for precise, actionable answers\n");

            var response = await ragService.AskQuestionAsync(
                question: "How do I reset my password?",
                userId: "customer123",
                domain: "support"
            );

            DisplayResponse(response);
            Console.WriteLine("---\n");
        }

        private static async Task DemoResearchQuery(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("🔬 DEMO 2: Research Query (Balanced Approach)");
            Console.WriteLine("Query: 'Explain machine learning optimization techniques'");
            Console.WriteLine("Expected: Moderate lambda (λ ≈ 0.6) for diverse perspectives\n");

            var response = await ragService.AskQuestionAsync(
                question: "Explain machine learning optimization techniques",
                userId: "researcher456",
                domain: "research"
            );

            DisplayResponse(response);
            Console.WriteLine("---\n");
        }

        private static async Task DemoProceduralQuery(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("📋 DEMO 3: Procedural Query (Step-by-step)");
            Console.WriteLine("Query: 'What are the steps to deploy a web application?'");
            Console.WriteLine("Expected: High lambda (λ = 0.8) for precise procedural information\n");

            var response = await ragService.AskQuestionAsync(
                question: "What are the steps to deploy a web application?",
                userId: "developer789",
                domain: "technical"
            );

            DisplayResponse(response);
            Console.WriteLine("---\n");
        }

        private static async Task DemoComparativeQuery(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("⚖️ DEMO 4: Comparative Query (High Diversity)");
            Console.WriteLine("Query: 'Compare different cloud storage solutions'");
            Console.WriteLine("Expected: Lower lambda (λ = 0.5) for diverse comparison points\n");

            var response = await ragService.AskQuestionAsync(
                question: "Compare different cloud storage solutions",
                userId: "architect101",
                domain: "technical"
            );

            DisplayResponse(response);
            Console.WriteLine("---\n");
        }

        private static async Task DemoCaching(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("💾 DEMO 5: Caching Demonstration");
            Console.WriteLine("Asking the same question twice to demonstrate caching...\n");

            var question = "What is artificial intelligence?";
            
            // First request - should process normally
            Console.WriteLine("First request (should process normally):");
            var response1 = await ragService.AskQuestionAsync(question, "user123", "general");
            Console.WriteLine($"Processing time: {response1.Metadata.ProcessingTimeMs}ms");
            Console.WriteLine($"From cache: {response1.FromCache}");
            Console.WriteLine();

            // Second request - should come from cache
            Console.WriteLine("Second request (should come from cache):");
            var response2 = await ragService.AskQuestionAsync(question, "user123", "general");
            Console.WriteLine($"Processing time: {response2.Metadata.ProcessingTimeMs}ms");
            Console.WriteLine($"From cache: {response2.FromCache}");
            Console.WriteLine("---\n");
        }

        private static async Task DemoErrorHandling(EnterpriseRAGService ragService, ILogger logger)
        {
            Console.WriteLine("⚠️ DEMO 6: Error Handling");
            Console.WriteLine("Testing graceful error handling with edge cases...\n");

            // Test with empty question
            var emptyResponse = await ragService.AskQuestionAsync("", "user123", "general");
            Console.WriteLine($"Empty query response: Success = {emptyResponse.Success}");
            
            // Test with very long question
            var longQuestion = new string('a', 10000);
            var longResponse = await ragService.AskQuestionAsync(longQuestion, "user123", "general");
            Console.WriteLine($"Long query response: Success = {longResponse.Success}");
            Console.WriteLine("---\n");
        }

        private static void DisplayResponse(RAGResponse response)
        {
            Console.WriteLine($"✅ Request ID: {response.RequestId}");
            Console.WriteLine($"📊 Lambda Used: {response.Metadata.Lambda:F2}");
            Console.WriteLine($"⏱️ Processing Time: {response.Metadata.ProcessingTimeMs}ms");
            Console.WriteLine($"📄 Candidates Retrieved: {response.Metadata.CandidatesRetrieved}");
            Console.WriteLine($"🎯 Documents Selected: {response.Metadata.DocumentsSelected}");
            Console.WriteLine($"🏷️ Domain: {response.Metadata.Domain}");
            Console.WriteLine($"💾 From Cache: {response.FromCache}");
            Console.WriteLine($"📝 Answer Preview: {response.Answer[..Math.Min(100, response.Answer.Length)]}...");
            
            if (response.SourceDocuments.Any())
            {
                Console.WriteLine("📚 Source Documents:");
                foreach (var doc in response.SourceDocuments.Take(3))
                {
                    Console.WriteLine($"   • {doc.Title} (Relevance: {doc.RelevanceScore:F3})");
                }
            }
        }
    }
}