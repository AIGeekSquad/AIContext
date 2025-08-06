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

### Seeing the Problem in Action

Let's examine a concrete example that demonstrates how traditional semantic search creates the clustering problem. We'll simulate an e-commerce product search where a customer searches for "wireless headphones" and see how traditional similarity-based ranking fails to provide diverse results.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// E-commerce product search - demonstrating the clustering problem

// Product catalog with similarity scores to query "wireless headphones"
var products = new[]
{
    new { Name = "Sony WH-1000XM4 Wireless Headphones", Similarity = 0.95, Category = "Audio" },
    new { Name = "Bose QuietComfort Wireless Headphones", Similarity = 0.93, Category = "Audio" },
    new { Name = "Apple AirPods Pro Wireless Earbuds", Similarity = 0.91, Category = "Audio" },
    new { Name = "Wireless Phone Charger", Similarity = 0.45, Category = "Accessories" },
    new { Name = "Bluetooth Speaker", Similarity = 0.42, Category = "Audio" },
    new { Name = "USB-C Cable", Similarity = 0.15, Category = "Accessories" }
};

Console.WriteLine("Query: 'wireless headphones'");
Console.WriteLine("\nTraditional search (top 3 most similar):");

var traditionalResults = products
    .OrderByDescending(p => p.Similarity)
    .Take(3);
    
foreach (var product in traditionalResults)
{
    Console.WriteLine($"• {product.Name} (similarity: {product.Similarity})");
}

var uniqueCategories = traditionalResults.Select(p => p.Category).Distinct().Count();
Console.WriteLine($"\nProblem: Only {uniqueCategories} category represented - missing accessories!");
```

**What this example reveals:** Traditional semantic search returns three highly similar products (all headphones with similarity scores above 0.9), but completely misses complementary accessories like wireless chargers that customers often need. The algorithm prioritizes similarity over usefulness, giving users redundant options instead of a comprehensive shopping experience.

This clustering problem becomes even more pronounced in RAG systems where you have limited context space. When your language model can only process 5 documents, having 3 of them say essentially the same thing wastes 60% of your available context.

> **🔍 See the clustering problem in action:** The [`examples/MMRClusteringProblemDemo.cs`](examples/MMRClusteringProblemDemo.cs) file demonstrates this exact issue with a customer support scenario, showing how traditional search returns 3 similar "password reset" solutions while MMR provides diverse approaches across authentication, support, and technical categories.

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

Now let's see how MMR works in practice with a customer support scenario. This example demonstrates how MMR can help a support system provide comprehensive troubleshooting steps instead of repetitive suggestions that all address the same root cause.

```csharp
using System;
using System.Linq;
using AiGeekSquad.AIContext.Ranking;
using MathNet.Numerics.LinearAlgebra;

// Customer support ticket routing - select diverse solution approaches

// Available solutions for "app crashes on startup"
// Each vector represents different solution categories: [basic_fixes, system_issues, hardware_problems]
var solutions = new[]
{
    ("Clear app cache and data", Vector<double>.Build.DenseOfArray([0.9, 0.1, 0.0])),
    ("Restart the application", Vector<double>.Build.DenseOfArray([0.85, 0.15, 0.0])),
    ("Reinstall the app", Vector<double>.Build.DenseOfArray([0.88, 0.12, 0.0])),
    ("Check system requirements", Vector<double>.Build.DenseOfArray([0.3, 0.8, 0.1])),
    ("Update device drivers", Vector<double>.Build.DenseOfArray([0.2, 0.1, 0.9])),
    ("Contact technical support", Vector<double>.Build.DenseOfArray([0.1, 0.9, 0.1]))
};

// User query: "app won't start"
var query = Vector<double>.Build.DenseOfArray([0.9, 0.2, 0.1]);

Console.WriteLine("=== Support Ticket: 'App won't start' ===\n");

// Apply MMR to get diverse solutions
var mmrResults = MaximumMarginalRelevance.ComputeMMR(
    vectors: solutions.Select(s => s.Item2).ToList(),
    query: query,
    lambda: 0.7,  // Balance relevance with diversity
    topK: 3
);

Console.WriteLine("Recommended solutions:");
foreach (var (index, score) in mmrResults)
{
    Console.WriteLine($"• {solutions[index].Item1}");
}
```

**What MMR accomplished here:** Instead of suggesting three variations of "restart/reinstall/clear cache" (which traditional similarity search would do), MMR selected solutions from different troubleshooting categories. The user gets a comprehensive troubleshooting path: try basic fixes first, then check system compatibility, and finally address potential hardware issues.

This is the core value of MMR - it ensures your AI assistant provides well-rounded guidance rather than tunnel vision on the most obvious solutions.

The key changes from traditional retrieval:

1. Get more candidates initially (20 instead of 5)
2. Apply MMR to select the final set
3. Use lambda = 0.7 as a starting point

> **🎯 From Concept to Production**
> The examples above show MMR's core concepts, but how do you build this into a real application? The [`examples/`](examples/) folder contains complete, production-ready implementations:
>
> - **[`SupportTicketRouter.cs`](examples/SupportTicketRouter.cs)** - Customer support routing (similar to the example above)
> - **[`ProductSearchDemo.cs`](examples/ProductSearchDemo.cs)** - E-commerce product recommendations
> - **[`EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs)** - Full enterprise RAG service with MMR
>
> These files include dependency injection setup, error handling, logging, and everything you need to run them in your own projects.

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

For production systems, consider implementing dynamic lambda selection based on query characteristics. We'll see how this works in practice in the complete implementation that follows.

Now that you understand how to choose the right lambda value, let's see how all these pieces fit together in a production-ready system.

## Complete RAG Pipeline with MMR

> **🔄 Drop-in replacement:** Upgrade your existing RAG system in 30 minutes.

The code snippets above show the core MMR concepts, but how do they all fit together in a production system? Let's bridge from theory to practice with a complete, enterprise-ready implementation.

> **💡 Complete Implementation Available**
> While this section shows key concepts, you can find the **complete, runnable implementation** in the examples folder:
> - **[`examples/EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs)** - Full production-ready service (565 lines)
> - **[`examples/EnterpriseRAGServiceDemo.cs`](examples/EnterpriseRAGServiceDemo.cs)** - Working demo with multiple scenarios (222 lines)
>
> These files include everything: dependency injection setup, comprehensive error handling, logging, caching, mock implementations for testing, and detailed documentation.

### From Snippets to Production: What You'll Find

The complete implementation in [`examples/EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs) demonstrates how all the concepts we've discussed come together in a production-ready system:

**🏗️ Enterprise Architecture:**
- Dependency injection ready with [`IServiceCollection`](examples/EnterpriseRAGService.cs:373) configuration
- Comprehensive error handling and logging throughout
- Request tracing with unique IDs for observability
- Configurable options via [`RAGServiceOptions`](examples/EnterpriseRAGService.cs:268)

**🧠 Intelligent Processing Pipeline:**
- Two-stage retrieval: retrieve 25 candidates, select 5 with MMR
- Adaptive lambda selection via [`GetOptimalLambda()`](examples/EnterpriseRAGService.cs:172)
- Smart caching with TTL to reduce API costs
- Response metadata for monitoring and debugging

**🔧 Production Features:**
- Mock implementations for testing ([`MockEmbeddingGenerator`](examples/EnterpriseRAGService.cs:459), [`MockVectorDatabase`](examples/EnterpriseRAGService.cs:485), [`MockLanguageModel`](examples/EnterpriseRAGService.cs:548))
- Comprehensive response types with source attribution
- Cancellation token support for async operations
- Performance optimization patterns

### Key Implementation Highlights

Here are the essential patterns from the complete implementation that make it production-ready:

#### 1. Adaptive Lambda Selection

The [`GetOptimalLambda()`](examples/EnterpriseRAGService.cs:172) method automatically adjusts the relevance-diversity balance:

```csharp
// Query-based selection
if (questionLower.Contains("how to") || questionLower.Contains("steps"))
    return 0.8; // Precision needed for procedures

if (questionLower.Contains("compare") || questionLower.Contains("different"))
    return 0.5; // Diversity needed for comparisons

// Domain-based defaults
return domain.ToLowerInvariant() switch
{
    "support" => 0.8,    // Customer support needs precise answers
    "research" => 0.6,   // Research benefits from diverse perspectives
    "legal" => 0.9,      // Legal queries need high precision
    _ => 0.7             // Balanced default
};
```

#### 2. Two-Stage Retrieval Pipeline

The complete [`AskQuestionAsync()`](examples/EnterpriseRAGService.cs:60) method shows the full request lifecycle:

```csharp
// 1. Check cache first
var cacheKey = ComputeCacheKey(question, domain);
if (_cache.TryGetValue(cacheKey, out RAGResponse cachedResponse))
    return cachedResponse;

// 2. Generate query embedding
var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(question);

// 3. Retrieve candidates (cast wide net)
var candidates = await _vectorDatabase.SearchAsync(queryEmbedding, limit: 25);

// 4. Apply MMR for intelligent selection
var lambda = GetOptimalLambda(question, domain);
var selectedDocs = MaximumMarginalRelevance.ComputeMMR(
    vectors: candidates.Select(c => c.Embedding).ToList(),
    query: queryEmbedding,
    lambda: lambda,
    topK: 5
);

// 5. Generate response with comprehensive metadata
var response = new RAGResponse { /* ... detailed response object ... */ };
```

#### 3. Ready-to-Run Demo

The [`EnterpriseRAGServiceDemo.cs`](examples/EnterpriseRAGServiceDemo.cs) file shows the service in action with different query types:

- **Customer Support**: "How do I reset my password?" (λ = 0.8)
- **Research**: "Explain machine learning optimization" (λ = 0.6)
- **Procedural**: "Steps to deploy a web application" (λ = 0.8)
- **Comparative**: "Compare cloud storage solutions" (λ = 0.5)
- **Caching demonstration** with performance metrics
- **Error handling** with edge cases

### Getting Started with the Complete Example

1. **Explore the implementation**: Start with [`examples/EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs) to see the full service
2. **Run the demo**: Use [`examples/EnterpriseRAGServiceDemo.cs`](examples/EnterpriseRAGServiceDemo.cs) to see it in action
3. **Adapt for your needs**: Replace the mock implementations with your actual services
4. **Configure for production**: Adjust the [`RAGServiceOptions`](examples/EnterpriseRAGService.cs:268) for your use case

**What this architecture demonstrates:** This isn't just about adding MMR to your existing system - it's about building a robust, scalable service that intelligently adapts its behavior based on the type of question being asked. The complete implementation shows how adaptive lambda selection, two-stage retrieval, and enterprise patterns work together to create a production-ready RAG system.

The two-stage retrieval pattern (retrieve 25 candidates, select 5 with MMR) is particularly important for performance. You get the benefits of MMR's intelligent selection without the computational overhead of comparing every document in your database.

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

While we've focused on traditional RAG systems, MMR's power extends far beyond document retrieval. The same relevance-diversity balance that improves RAG responses can enhance many other AI applications. Let's explore three practical scenarios where MMR creates significantly better user experiences by preventing the "echo chamber" effect.

### 1. Recommendation Systems

E-commerce platforms often struggle with recommendation diversity - users see the same type of product repeatedly instead of discovering new categories. Here's how MMR solves this:

```csharp
// Recommend diverse products based on user preferences
var recommendations = MaximumMarginalRelevance.ComputeMMR(
    vectors: productEmbeddings,
    query: userPreferenceVector,
    lambda: 0.4,  // Favor diversity for discovery
    topK: 10
);
```

**The impact:** Instead of showing 10 similar smartphones to someone who bought one phone, MMR might recommend 3 phones, 2 cases, 2 chargers, 2 screen protectors, and 1 wireless speaker. Users discover complementary products they didn't know they needed.

### 2. Content Curation

Newsletter editors and content managers face the challenge of keeping audiences engaged with varied, interesting content rather than repetitive articles on the same narrow topics:

```csharp
// Select diverse articles for a newsletter
var curatedArticles = MaximumMarginalRelevance.ComputeMMR(
    vectors: articleEmbeddings,
    query: topicVector,
    lambda: 0.3,  // High diversity for engaging content
    topK: 5
);
```

**The impact:** A tech newsletter about "AI developments" won't just feature 5 articles about ChatGPT. MMR ensures coverage of different AI domains: one on language models, one on computer vision, one on robotics, one on AI ethics, and one on industry applications.

### 3. Research Paper Discovery

Academic researchers need to explore different perspectives and methodologies within their field, not just papers that use identical approaches:

```csharp
// Find papers covering different aspects of a research area
var diversePapers = MaximumMarginalRelevance.ComputeMMR(
    vectors: paperEmbeddings,
    query: researchQuery,
    lambda: 0.6,  // Balance relevance with topical diversity
    topK: 15
);
```

**The impact:** A search for "machine learning optimization" returns papers covering different optimization techniques (gradient descent, evolutionary algorithms, reinforcement learning), different domains (computer vision, NLP, robotics), and different evaluation metrics - giving researchers a comprehensive view of the field.

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
- Begin with λ = 0.7 (see the [Lambda selection guide](#choosing-the-right-lambda-value) above for details)
- Replace just one retrieval call initially to test impact
- Measure user feedback before expanding

**Monitor What Matters:**
- Track user satisfaction and task completion rates
- Count unique topics in selected documents
- Monitor follow-up question frequency
- A/B test different lambda values with real users

**Scale Thoughtfully:**
- Implement caching and two-stage filtering (see the [complete implementation](examples/EnterpriseRAGService.cs) for production patterns)
- Document optimal lambda values for your specific use cases
- Monitor performance impact as you scale

> **🚀 Ready to implement?** The [`examples/EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs) file contains all these best practices in a production-ready implementation you can adapt for your needs.

The key is to start simple, measure the impact, and iterate based on real user behavior rather than assumptions.

## What's Next: Your Advanced RAG Roadmap

> **🚀 The future of RAG:** Intelligent context selection is just the beginning of building truly helpful AI assistants.

### Complementary Techniques That Amplify MMR

Once you've mastered basic MMR implementation, these advanced patterns can take your RAG system to the next level. Each technique addresses specific challenges that emerge in sophisticated AI applications, combining MMR with other cutting-edge approaches for even better results.

#### **1. Semantic Chunking + MMR**

Traditional chunking splits documents at arbitrary boundaries, but semantic chunking preserves meaning. When combined with MMR, you get the best of both worlds: meaningful chunks that cover diverse aspects of your topic.

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

**Impact:** Instead of getting 8 chunks that all discuss the same concept from a long document, you get chunks covering different aspects - introduction, methodology, results, and conclusions. This gives users a complete understanding rather than repetitive details.

> **💡 See it in action:** The [`EnterpriseRAGServiceDemo.cs`](examples/EnterpriseRAGServiceDemo.cs) file demonstrates these advanced patterns with working examples you can run and modify.

#### **2. Multi-Vector Retrieval + MMR**

Modern applications often contain different types of content - text, code, images, structured data. This technique uses specialized embeddings for each content type, then applies MMR across the combined results.

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

**Impact:** A developer asking "How do I implement authentication?" gets both conceptual explanations AND working code examples, not just 5 similar code snippets or 5 theoretical articles. The diversity spans content types, not just topics.

#### **3. Two-Stage MMR Pipeline**

For complex domains with many subtopics, a single MMR pass might not provide enough diversity. This technique applies MMR twice: first for broad topic coverage, then for focused selection within each topic area.

```csharp
// Stage 1: Broad retrieval with MMR for topic diversity
var topicDiverseResults = MaximumMarginalRelevance.ComputeMMR(
    vectors: allCandidates, query: queryEmbedding, lambda: 0.4, topK: 15);

// Stage 2: Focused selection within diverse topics
var finalSelection = MaximumMarginalRelevance.ComputeMMR(
    vectors: topicDiverseResults.Select(r => r.vector).ToList(),
    query: queryEmbedding, lambda: 0.8, topK: 5);
```

**Impact:** A query about "cloud architecture" first ensures coverage of different cloud aspects (security, scalability, cost, deployment), then selects the most relevant document from each area. You get both breadth AND depth without sacrificing either.

These advanced techniques represent the cutting edge of intelligent content selection, where MMR becomes part of a larger strategy for building truly helpful AI systems.

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

> **🚀 Ready to get started?**
> Everything you need is in the [`examples/`](examples/) folder:
> - **[`EnterpriseRAGService.cs`](examples/EnterpriseRAGService.cs)** - Complete production service
> - **[`EnterpriseRAGServiceDemo.cs`](examples/EnterpriseRAGServiceDemo.cs)** - Working demo with multiple scenarios
> - **[`MMRExample.cs`](examples/MMRExample.cs)** - Basic MMR usage patterns
>
> Clone the repository, run the demos, and adapt the code for your specific use case.

The goal isn't just to implement a new algorithm - it's to build AI systems that genuinely help people accomplish their goals. MMR is one powerful technique to get you there.