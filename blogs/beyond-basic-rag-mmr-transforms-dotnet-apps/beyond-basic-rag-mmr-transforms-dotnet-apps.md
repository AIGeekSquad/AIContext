# Beyond Basic RAG: How Maximum Marginal Relevance Transforms Your .NET Applications

*Building on the foundation: Taking RAG systems from good to great with intelligent context selection*

---

Your AI assistant just confidently told a user that the solution to their problem is "restart the service" - three times in a row, using slightly different words each time. Meanwhile, the specific configuration changes they actually needed never made it into the response.

If you've built a RAG system, you've probably seen this happen. Your vector search finds the most relevant documents, but they all say basically the same thing. You end up with repetitive information instead of comprehensive answers.

This post explores a technique called **Maximum Marginal Relevance (MMR)** that can help solve this problem. We'll look at why it happens, how MMR works, and how to implement it in your .NET applications.

## Why Context Selection Matters in RAG Systems

When you build a RAG system, you're working with a fundamental constraint: language models can only process a limited amount of data at once. This means you need to be selective about which documents you include in your context.

### The Context Window Challenge

Most language models have limited context windows:

When you can only include a few documents, each one needs to add unique value. If three of your five documents are saying the same thing, you're wasting valuable context space.

### What Happens When Context Selection Goes Wrong

Poor context selection creates several problems:

**Response Quality Issues:**
- You get generic answers instead of specific solutions
- Important information gets left out
- Users receive contradictory advice

**Real-World Examples:**
- A medical system mixing Type 1 and Type 2 diabetes information
- A legal system combining precedents from different jurisdictions
- A support system showing upgrade information when someone wants to cancel

**User Experience Impact:**
- Users need to ask follow-up questions
- People lose trust in the system
- Task completion rates drop

The goal then is to select documents that work together to provide comprehensive, non-redundant information.

## The Problem with Traditional Semantic Search

Traditional RAG systems use semantic search to find the most relevant documents. This works by finding documents that are most similar to your query. While this sounds logical, it creates a problem: you often end up with very similar documents.

Here's why this happens: if your query is about "optimizing application performance," semantic search will find documents with the highest similarity scores. These often cluster around the same topics, giving you multiple documents about memory management but missing other important aspects like database optimization or caching strategies.

![](./rag-vs-mmr.png)

### A Simple Example

Let's say you have these documents in your system:
1. "Memory Management Best Practices"
2. "Garbage Collection Tuning Guide"
3. "Heap Optimization Strategies"
4. "Database Query Optimization"
5. "Caching Strategies"

When someone asks "How do I optimize my application?", traditional semantic search might return the first three documents because they're all highly relevant to "optimization." But now you have three documents about memory management and nothing about databases or caching.

```csharp
// This is how traditional retrieval works
public async Task<List<Document>> GetRelevantDocuments(string query)
{
    var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
    
    var allDocuments = await _vectorDatabase.SearchAsync(queryEmbedding, limit: 20);
    
    // Just take the top 5 most similar - this causes clustering
    return allDocuments
        .OrderByDescending(doc => CalculateSimilarity(queryEmbedding, doc.Embedding))
        .Take(5)
        .ToList();
}
```

The result is that users get repetitive information instead of comprehensive coverage of their topic.

## What is Maximum Marginal Relevance?

Maximum Marginal Relevance (MMR) is a technique that balances two goals:
1. **Relevance**: How well does this document match what the user asked?
2. **Diversity**: How different is this document from what we've already selected?

Instead of just picking the most relevant documents, MMR tries to pick documents that are both relevant AND different from each other.

### The MMR Formula

MMR uses a simple formula to score each document:

```
MMR Score = λ × Relevance + (1-λ) × Diversity
```

The `λ` (lambda) parameter controls the balance:
- **λ = 1.0**: Only care about relevance (same as traditional search)
- **λ = 0.7**: Mostly care about relevance, but include some variety
- **λ = 0.5**: Equal balance between relevance and diversity
- **λ = 0.0**: Only care about diversity

For most applications, starting with λ = 0.7 works well. This means you still prioritize relevant documents, but you'll avoid selecting multiple documents that say the same thing.

## Implementing MMR

Let's walk through implementing MMR in your existing RAG system. The good news is that if you already have a working RAG implementation, adding MMR is straightforward.

### Installing the Package

For simplicity, in this post, we're focusing on the use of MMR rather than the actual implementation. As a result, we've packaged an implementation of MMR in the [AiGeekSquad.AIContext](https://www.nuget.org/packages?q=aigeeksquad.aicontext) NuGet package.   

Start by creating a C# console application and adding the [AiGeekSquad.AIContext](https://www.nuget.org/packages?q=aigeeksquad.aicontext) NuGet package to it:

```bash
dotnet add package AiGeekSquad.AIContext
```

### Basic Implementation

Here's how to modify your existing document retrieval to use MMR:

```csharp
using AiGeekSquad.AIContext.Ranking;

public class RAGService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorDatabase _vectorDb;
    
    public async Task<List<Document>> GetDocumentsWithMMR(string userQuestion)
    {
        // Step 1: Generate embedding for the user's question
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(userQuestion);
        
        // Step 2: Get more candidates than you need (cast a wider net)
        var candidates = await _vectorDb.SearchAsync(queryEmbedding, topK: 20);
        
        // Step 3: Use MMR to select diverse, relevant documents
        var selectedIndices = MaximumMarginalRelevance.ComputeMMR(
            vectors: candidates.Select(c => c.Embedding).ToList(),
            query: queryEmbedding,
            lambda: 0.7,  // Start with this value
            topK: 5       // How many documents you actually want
        );
        
        // Step 4: Return the selected documents
        return selectedIndices
            .Select(result => candidates[result.index])
            .ToList();
    }
}
```

Steps 1, 2, and 4, you're already doing as part of your traditional RAG pipeline.

The key changes from traditional retrieval:

1. Get more candidates initially (20 instead of 5)
2. Apply MMR to select the final set
3. Use lambda = 0.7 as a starting point

## Choosing the Right Lambda Value

The lambda parameter controls how MMR balances relevance and diversity. Here's a comprehensive guide to selecting the optimal value:

### Lambda Values and Their Effects

| Lambda Value | Relevance vs Diversity Balance | Best Use Cases |
|:------------:|:------------------------------|:---------------|
| **λ = 0.9**  | 🎯 **High relevance**, low diversity | Precise answers, troubleshooting, FAQ systems |
| **λ = 0.7**  | ⚖️ **Balanced** (recommended start) | General-purpose RAG, most applications |
| **λ = 0.5**  | 🔄 **Equal** relevance and diversity | Research queries, comparative analysis |
| **λ = 0.3**  | 🌟 **High diversity**, some relevance | Content discovery, brainstorming, exploration |

### Domain-Specific Lambda Recommendations

**Start with 0.7** for most applications, then adjust based on your specific needs:

- **Customer Support**: λ = 0.8 (users want accurate, specific answers)
- **Research Tools**: λ = 0.6 (users benefit from diverse perspectives)
- **Content Discovery**: λ = 0.4 (users want to discover new things)
- **Technical Documentation**: λ = 0.8 (accuracy is paramount)

### Adaptive Lambda Selection

For production systems, consider implementing dynamic lambda selection based on query characteristics (see the [`GetOptimalLambda`](blogs/beyond-basic-rag-mmr-transforms-dotnet-apps/beyond-basic-rag-mmr-transforms-dotnet-apps.md:282) method in the complete implementation below).

Now that you understand how to choose the right lambda value, let's see how all these pieces fit together in a production-ready system.

## Complete RAG Pipeline with MMR

> **🔄 Drop-in replacement:** Upgrade your existing RAG system in 30 minutes.

Building on the basic implementation above, here's how to create an enterprise-grade RAG system with MMR. We'll break this down into manageable components:

### 1. Service Architecture & Dependencies

First, let's establish the foundational structure:

```csharp
public class EnterpriseRAGService
{
    // Core dependencies for production RAG system
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorDatabase _vectorDb;
    private readonly ILanguageModel _llm;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EnterpriseRAGService> _logger;
    
    public async Task<RAGResponse> AskQuestionAsync(
        string question,
        string userId = null,
        string domain = "general")
    {
        // Create unique request ID for tracing across distributed systems
        var requestId = Guid.NewGuid().ToString("N")[..8];
        using var scope = _logger.BeginScope("RequestId: {RequestId}", requestId);
        
        try
        {
            // PHASE 1: Check for cached responses to avoid redundant processing
            if (await TryGetCachedResponse(question) is RAGResponse cached)
            {
                return cached;
            }
            
            // PHASE 2: Convert question to vector representation
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
            
            // PHASE 3: Retrieve candidate documents from vector database
            var candidates = await RetrieveCandidates(queryEmbedding);
            if (!candidates.Any())
            {
                return CreateErrorResponse("I don't have enough information to answer that question.", requestId);
            }
            
            // PHASE 4: Apply MMR to select diverse, relevant documents
            var selectedDocuments = await ApplyMMRSelection(candidates, queryEmbedding, question, domain);
            
            // PHASE 5: Generate final response using selected context
            var result = await GenerateResponse(question, selectedDocuments, requestId);
            
            // Cache successful responses for future use
            await CacheResponse(question, result);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG query failed for question: {Question}", question);
            return CreateErrorResponse("I encountered an error processing your question. Please try again.", requestId);
        }
    }
}
```

### 2. Caching Strategy Implementation

```csharp
private async Task<RAGResponse> TryGetCachedResponse(string question)
{
    // Use question hash as cache key for consistent lookups
    var cacheKey = $"query:{question.GetHashCode():X}";
    
    if (_cache.TryGetValue(cacheKey, out RAGResponse cachedResponse))
    {
        _logger.LogInformation("Returned cached response for similar query");
        return cachedResponse;
    }
    
    return null; // No cached response found
}

private async Task CacheResponse(string question, RAGResponse result)
{
    var cacheKey = $"query:{question.GetHashCode():X}";
    // Cache for 15 minutes to balance freshness with performance
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
}
```

### 3. Document Retrieval & MMR Selection

```csharp
private async Task<List<DocumentResult>> RetrieveCandidates(float[] queryEmbedding)
{
    // Retrieve more candidates than needed to give MMR better selection options
    // 25 candidates allows for good diversity while maintaining performance
    return await _vectorDb.SearchAsync(queryEmbedding, topK: 25);
}

private async Task<List<DocumentResult>> ApplyMMRSelection(
    List<DocumentResult> candidates,
    float[] queryEmbedding,
    string question,
    string domain)
{
    // Dynamically choose lambda based on question type and domain
    var lambda = GetOptimalLambda(question, domain);
    
    // Apply MMR algorithm to balance relevance with diversity
    var selectedIndices = MaximumMarginalRelevance.ComputeMMR(
        vectors: candidates.Select(c => c.Embedding).ToList(),
        query: queryEmbedding,
        lambda: lambda,
        topK: 5 // Final number of documents to include in context
    );
    
    // Return the selected documents in order of MMR ranking
    return selectedIndices
        .Select(result => candidates[result.index])
        .ToList();
}
```

### 4. Response Generation & Error Handling

```csharp
private async Task<RAGResponse> GenerateResponse(
    string question,
    List<DocumentResult> contextDocuments,
    string requestId)
{
    // Build context string with clear source attribution
    var contextText = BuildContextWithMetadata(contextDocuments);
    
    // Generate response using language model
    var response = await _llm.GenerateResponseAsync(question, contextText);
    
    // Create comprehensive response object with metadata
    var result = new RAGResponse
    {
        Answer = response,
        SourceDocuments = contextDocuments.Select(d => d.Title).ToList(),
        Lambda = GetOptimalLambda(question, "general"), // Store lambda used
        CandidateCount = contextDocuments.Count,
        Success = true,
        RequestId = requestId
    };
    
    _logger.LogInformation(
        "RAG query completed successfully. Lambda: {Lambda}, Sources: {SourceCount}",
        result.Lambda, contextDocuments.Count);
        
    return result;
}

private RAGResponse CreateErrorResponse(string message, string requestId)
{
    return new RAGResponse
    {
        Answer = message,
        Success = false,
        RequestId = requestId
    };
}

private string BuildContextWithMetadata(List<DocumentResult> documents)
{
    // Format context with clear source numbering for LLM processing
    return string.Join("\n\n", documents.Select((doc, index) =>
        $"Source {index + 1} ({doc.Title}):\n{doc.Content}"));
}
```

### 5. Adaptive Lambda Selection

```csharp
private double GetOptimalLambda(string question, string domain)
{
    // Apply domain-specific lambda values (see section "Choosing the Right Lambda Value")
    if (question.Contains("how to") || question.Contains("steps"))
        return 0.8; // Procedural questions need precision
        
    if (question.Contains("compare") || question.Contains("different"))
        return 0.5; // Comparison questions benefit from diversity
        
    return domain switch
    {
        "support" => 0.8,
        "research" => 0.6,
        "discovery" => 0.4,
        _ => 0.7 // Default recommended starting value
    };
}
```

### 6. Response Data Model

```csharp
public class RAGResponse
{
    public string Answer { get; set; }
    public List<string> SourceDocuments { get; set; } = new();
    public double Lambda { get; set; }
    public int CandidateCount { get; set; }
    public bool Success { get; set; }
    public string RequestId { get; set; }
}
```

**Enterprise features you get:**
- **Smart caching**: Avoid redundant embedding generation
- **Error resilience**: Graceful handling of failures
- **Performance tracking**: Monitor selection quality
- **Adaptive lambda**: Automatic tuning based on question type
- **Source attribution**: Full transparency for users

With this modular architecture, you can easily test, maintain, and scale each component independently. The caching layer alone can significantly reduce costs by avoiding redundant API calls to embedding services.

## Performance Considerations

One common concern when implementing MMR is the computational overhead of comparing documents against each other. Let's examine the real-world performance characteristics and optimization strategies:

MMR requires comparing documents against each other, but the performance impact is minimal for typical applications:

**Benchmarked Performance:**
- **1,000 vectors, 10 dimensions, topK=5**: ~2ms
- **Memory allocation**: ~120KB per 1,000 vectors
- **GC pressure**: Minimal

### Optimization Strategies

For large-scale applications, implement these optimizations in order of impact:

1. **Two-stage filtering**: Retrieve 100 candidates from your vector database, then apply MMR to select the final 5
2. **Smart caching**: Cache query embeddings for frequently asked questions (implemented in the enterprise example above)
3. **Vector normalization**: Improves both calculation quality and speed
4. **Lambda tuning**: Higher values (λ ≥ 0.8) reduce diversity calculations

## Advanced Use Cases: Beyond Simple RAG

While we've focused on traditional RAG systems, MMR's power extends far beyond document retrieval. The same relevance-diversity balance that improves RAG responses can enhance many other AI applications:

### 1. Recommendation Systems
```csharp
// Recommend diverse products based on user preferences
var recommendations = MaximumMarginalRelevance.ComputeMMR(
    vectors: productEmbeddings,
    query: userPreferenceVector,
    lambda: 0.4,  // Favor diversity for discovery
    topK: 10
);
```

### 2. Content Curation
```csharp
// Select diverse articles for a newsletter
var curatedArticles = MaximumMarginalRelevance.ComputeMMR(
    vectors: articleEmbeddings,
    query: topicVector,
    lambda: 0.3,  // High diversity for engaging content
    topK: 5
);
```

### 3. Research Paper Discovery
```csharp
// Find papers covering different aspects of a research area
var diversePapers = MaximumMarginalRelevance.ComputeMMR(
    vectors: paperEmbeddings,
    query: researchQuery,
    lambda: 0.6,  // Balance relevance with topical diversity
    topK: 15
);
```

These examples demonstrate MMR's versatility across different domains where balancing relevance with diversity creates better user experiences.

## Measuring MMR Success

Implementing MMR is just the first step - you need to validate that it's actually improving your application. Here's how to measure success across both technical performance and user experience:

### Technical Metrics to Track

These metrics help you understand if MMR is actually improving your document selection:

**Diversity Metrics:**
- Count how many unique topics appear in your selected documents
- Calculate the average similarity between selected documents (lower = more diverse)
- Track how many different document categories are represented

**Relevance Metrics:**
- Measure the average similarity between your query and selected documents
- Track whether the most relevant document is still being selected
- Monitor if relevance scores drop too much when optimizing for diversity

### User Experience Metrics

These metrics tell you if the changes are actually helping your users:

**Direct Feedback:**
- User satisfaction surveys asking about response quality
- Thumbs up/down ratings on responses
- "This answered my question" vs "This didn't help" feedback

**Behavioral Metrics:**
- Task completion rates (did users accomplish what they set out to do?)
- Follow-up question frequency (fewer follow-ups usually means better initial responses)
- Session length and engagement with responses
- Return user rates

### Simple A/B Testing Approach

The best way to validate MMR is to test it against your current system:

1. **Split your traffic**: Send 50% of queries through traditional retrieval, 50% through MMR
2. **Track the metrics above**: Compare the two groups over a few weeks
3. **Test different lambda values**: Try 0.5, 0.7, and 0.9 to see which works best for your users
4. **Ask users directly**: Simple surveys can give you quick insights

### Quick Wins to Measure

Some things you can check immediately without building complex analytics:

- **Topic diversity**: Manually review 20 responses from each system and count unique topics covered
- **User feedback**: Monitor existing feedback channels for complaints about repetitive or incomplete answers
- **Response length**: MMR often leads to more comprehensive responses as users get better context

## Implementation Best Practices

Ready to implement MMR in your own system? Here's a practical roadmap that minimizes risk while maximizing the chance of success:

**Start Simple:**
- Begin with λ = 0.7 (see [Lambda selection guide](blogs/beyond-basic-rag-mmr-transforms-dotnet-apps/beyond-basic-rag-mmr-transforms-dotnet-apps.md:161) for details)
- Replace just one retrieval call initially to test impact
- Measure user feedback before expanding

**Monitor What Matters:**
- Track user satisfaction and task completion rates
- Count unique topics in selected documents
- Monitor follow-up question frequency
- A/B test different lambda values with real users

**Scale Thoughtfully:**
- Implement caching and two-stage filtering (see [Performance section](blogs/beyond-basic-rag-mmr-transforms-dotnet-apps/beyond-basic-rag-mmr-transforms-dotnet-apps.md:319))
- Document optimal lambda values for your specific use cases
- Monitor performance impact as you scale

The key is to start simple, measure the impact, and iterate based on real user behavior rather than assumptions.

## What's Next: Your Advanced RAG Roadmap

> **🚀 The future of RAG:** Intelligent context selection is just the beginning of building truly helpful AI assistants.

### Complementary Techniques That Amplify MMR

#### **1. Semantic Chunking + MMR**
```csharp
// Break documents intelligently, then apply MMR for optimal diversity
var chunks = await _semanticChunker.ChunkDocumentAsync(document);
var selectedChunks = MaximumMarginalRelevance.ComputeMMR(
    vectors: chunks.Select(c => c.Embedding).ToList(),
    query: queryEmbedding,
    lambda: 0.6,  // Slightly favor diversity for chunk selection
    topK: 8
);
```
**Impact:** Get granular, diverse information without losing document context

#### **2. Multi-Vector Retrieval + MMR**
```csharp
// Use specialized embeddings for different content types
var textResults = await SearchTextEmbeddings(query);
var codeResults = await SearchCodeEmbeddings(query);
var combinedResults = textResults.Concat(codeResults);

var diverseSelection = MaximumMarginalRelevance.ComputeMMR(
    vectors: combinedResults.Select(r => r.Embedding).ToList(),
    query: queryEmbedding,
    lambda: 0.7,
    topK: 5
);
```
**Impact:** Comprehensive answers spanning multiple content modalities

#### **3. Two-Stage MMR Pipeline**
```csharp
// Stage 1: Broad retrieval with MMR for topic diversity
var topicDiverseResults = MaximumMarginalRelevance.ComputeMMR(
    vectors: allCandidates, query: queryEmbedding, lambda: 0.4, topK: 15);

// Stage 2: Focused selection within diverse topics
var finalSelection = MaximumMarginalRelevance.ComputeMMR(
    vectors: topicDiverseResults.Select(r => r.vector).ToList(),
    query: queryEmbedding, lambda: 0.8, topK: 5);
```
**Impact:** Perfect balance of breadth and depth in complex domains

### The Real Impact: Better Answers, Happier Users

MMR isn't just a technical optimization - it's about fundamentally improving how your users experience AI assistance. When you implement MMR, you're choosing to give users comprehensive, diverse information instead of repetitive responses.

**Think about your users' experience:**
- Instead of getting the same advice three different ways, they get a complete picture
- Instead of having to ask follow-up questions, they get thorough coverage the first time
- Instead of frustration with incomplete answers, they get the confidence to take action

**Your path forward:**
1. **Start small**: Try MMR on a few test queries to see the difference
2. **Measure the impact**: Track how often users need to ask follow-up questions
3. **Iterate**: Adjust your lambda value based on user feedback

The goal isn't just to implement a new algorithm - it's to build AI systems that genuinely help people accomplish their goals. MMR is one powerful technique to get you there.