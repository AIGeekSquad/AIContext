using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AiGeekSquad.AIContext.ContextRendering;
using FluentAssertions;
using FluentAssertions.Execution;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.AI;
using Xunit;
using IEmbeddingGenerator = AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator;
using ITokenCounter = AiGeekSquad.AIContext.Chunking.ITokenCounter;
using TextChunk = AiGeekSquad.AIContext.Chunking.TextChunk;

namespace AiGeekSquad.AIContext.Tests.ContextRendering;

public class ContextRendererTests
{
    private readonly FakeTokenCounter _tokenCounter;
    private readonly FakeEmbeddingGenerator _embeddingGenerator;

    public ContextRendererTests()
    {
        _tokenCounter = new FakeTokenCounter();
        _embeddingGenerator = new FakeEmbeddingGenerator();
    }

    // Fake implementations for testing
    private class FakeTokenCounter : ITokenCounter
    {
        public int CountTokens(string text) => text.Length / 4;

        public Task<int> CountTokensAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CountTokens(text));
        }

        public void Dispose() { }
    }

    private class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            // Create a simple embedding based on text content
            var hash = text.GetHashCode();
            var values = new double[]
            {
                Math.Abs((hash % 1000) / 1000.0),
                Math.Abs(((hash / 1000) % 1000) / 1000.0),
                Math.Abs(((hash / 1000000) % 1000) / 1000.0)
            };
            return Task.FromResult(Vector<double>.Build.DenseOfArray(values));
        }

        public async IAsyncEnumerable<Vector<double>> GenerateBatchEmbeddingsAsync(
            IEnumerable<string> texts,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var text in texts)
            {
                yield return await GenerateEmbeddingAsync(text, cancellationToken);
            }
        }
    }



    [Fact]
    public void Constructor_WithNullTokenCounter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextRenderer(null!, _embeddingGenerator));
    }

    [Fact]
    public void Constructor_WithNullEmbeddingGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextRenderer(_tokenCounter, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Act
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Assert
        renderer.Should().NotBeNull();
        renderer.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMessageAsync_WithValidChatMessage_AddsToContext()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var message = new ChatMessage(ChatRole.User, "Hello, world!");

        // Act
        await renderer.AddMessageAsync(message);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Content.Should().Contain("Hello, world!");
    }

    [Fact]
    public async Task AddMessageAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await renderer.AddMessageAsync(null!));
    }

    [Fact]
    public async Task AddMessagesAsync_WithMultipleChatMessages_AddsAllToContext()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hello"),
            new ChatMessage(ChatRole.Assistant, "Hi there"),
            new ChatMessage(ChatRole.User, "How are you?")
        };

        // Act
        await renderer.AddMessagesAsync(messages);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(3);
        renderer.Items[0].Content.Should().Contain("Hello");
        renderer.Items[1].Content.Should().Contain("Hi there");
        renderer.Items[2].Content.Should().Contain("How are you?");
    }

    [Fact]
    public async Task AddChunkAsync_WithValidChunk_AddsToContext()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var chunk = new TextChunk("This is a test document with some content.", 0, 43);

        // Act
        await renderer.AddChunkAsync(chunk);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Content.Should().Be(chunk.Text);
    }

    [Fact]
    public async Task AddChunkAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await renderer.AddChunkAsync(null!));
    }

    [Fact]
    public async Task RenderContextAsync_WithEmptyContext_ReturnsEmptyList()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Act
        var result = await renderer.RenderContextAsync("test query");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithNullOrEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)null!));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)""));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)"   "));
    }

    [Fact]
    public async Task RenderContextAsync_WithInvalidLambda_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", lambda: -0.1));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", lambda: 1.1));
    }

    [Fact]
    public async Task RenderContextAsync_WithInvalidFreshnessWeight_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", freshnessWeight: -0.1));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", freshnessWeight: 1.1));
    }

    [Fact]
    public async Task RenderContextAsync_WithNoTokenBudget_ReturnsAllItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.Assistant, "Hi there"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "How are you?"));

        // Act
        var result = await renderer.RenderContextAsync("test query", tokenBudget: null);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task RenderContextAsync_WithTokenBudget_RespectsLimit()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add messages with known token counts (length / 4 based on our fake)
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456")); // Content becomes "User: 1234567890123456" = 22 chars = 5 tokens
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456")); // 5 tokens
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456")); // 5 tokens

        // Act - limit to 10 tokens (should get at most 2 items)
        var result = await renderer.RenderContextAsync("test query", tokenBudget: 10);

        // Assert
        using var _ = new AssertionScope();
        result.Should().HaveCountLessThanOrEqualTo(2);
        var totalTokens = result.Sum(item => item.TokenCount);
        totalTokens.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task RenderContextAsync_WithHighLambda_PrioritizesRelevance()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add diverse messages
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about dogs"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about cats"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about birds"));

        // Act - high lambda should prioritize relevance over diversity
        var result = await renderer.RenderContextAsync("dogs and puppies", lambda: 0.9);

        // Assert
        result.Should().NotBeEmpty();
        // First result should be most relevant (about dogs)
        result[0].Content.Should().Contain("dogs");
    }

    [Fact]
    public async Task RenderContextAsync_WithLowLambda_PrioritizesDiversity()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add similar messages
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about dogs"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me more about dogs"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Dogs are great"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about cats")); // Different topic

        // Act - low lambda should prioritize diversity
        var result = await renderer.RenderContextAsync("dogs", lambda: 0.1, tokenBudget: 200);

        // Assert
        result.Should().NotBeEmpty();
        // Should include diverse items, not just all dog-related messages
        result.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task RenderContextAsync_WithMessagesQuery_CombinesMessagesIntoQuery()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.Assistant, "Hi"));

        var queryMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "What is AI?"),
            new ChatMessage(ChatRole.User, "Tell me more")
        };

        // Act
        var result = await renderer.RenderContextAsync(queryMessages);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithNullQueryMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await renderer.RenderContextAsync((IEnumerable<ChatMessage>)null!));
    }

    [Fact]
    public async Task RenderContextAsync_WithEmptyQueryMessages_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync(new List<ChatMessage>()));
    }

    [Fact]
    public async Task Clear_RemovesAllItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "World"));

        // Act
        renderer.Clear();

        // Assert
        renderer.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ContextItem_PreservesTimestamp()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var beforeTime = DateTime.UtcNow;

        // Act
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"));
        var afterTime = DateTime.UtcNow;

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Timestamp.Should().BeOnOrAfter(beforeTime);
        renderer.Items[0].Timestamp.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public async Task RenderContextAsync_WithFreshnessWeight_PrioritizesRecentItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add items with delays to ensure different timestamps
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Old message about dogs"));
        await Task.Delay(100); // Ensure different timestamp
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Recent message about cats"));

        // Act - high freshness weight should prioritize recent items
        var result = await renderer.RenderContextAsync("test query", freshnessWeight: 0.8, tokenBudget: 100);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithZeroFreshnessWeight_IgnoresTimestamps()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Old message"));
        await Task.Delay(100);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Recent message"));

        // Act - zero freshness weight should ignore timestamps
        var result = await renderer.RenderContextAsync("test query", freshnessWeight: 0.0);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddChunksAsync_WithMultipleChunks_AddsAllToContext()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var chunks = new List<TextChunk>
        {
            new TextChunk("This is a test document.", 0, 24),
            new TextChunk("It has multiple sentences.", 25, 51),
            new TextChunk("Each sentence is important.", 52, 79)
        };

        // Act
        await renderer.AddChunksAsync(chunks);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(3);
        renderer.Items[0].Content.Should().Be("This is a test document.");
        renderer.Items[1].Content.Should().Be("It has multiple sentences.");
        renderer.Items[2].Content.Should().Be("Each sentence is important.");
    }

    [Fact]
    public async Task AddChunksAsync_WithNullChunks_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await renderer.AddChunksAsync(null!));
    }

    [Fact]
    public void ContextItem_Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var content = "Test content";
        var embedding = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 });
        var tokenCount = 5;
        var timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var item = new ContextItem(content, embedding, tokenCount, timestamp);

        // Assert
        using var _ = new AssertionScope();
        item.Content.Should().Be(content);
        item.Embedding.Should().BeSameAs(embedding);
        item.TokenCount.Should().Be(tokenCount);
        item.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ContextItem_Constructor_WithNullTimestamp_UsesCurrentTime()
    {
        // Arrange
        var content = "Test content";
        var embedding = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 });
        var tokenCount = 5;
        var beforeTime = DateTime.UtcNow;

        // Act
        var item = new ContextItem(content, embedding, tokenCount, null);
        var afterTime = DateTime.UtcNow;

        // Assert
        using var _ = new AssertionScope();
        item.Timestamp.Should().BeOnOrAfter(beforeTime);
        item.Timestamp.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void ContextItem_Constructor_WithNullOrEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var embedding = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ContextItem(null!, embedding, 5));
        Assert.Throws<ArgumentException>(() => new ContextItem("", embedding, 5));
        Assert.Throws<ArgumentException>(() => new ContextItem("   ", embedding, 5));
    }

    [Fact]
    public void ContextItem_Constructor_WithNullEmbedding_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ContextItem("content", null!, 5));
    }

    [Fact]
    public void ContextItem_Constructor_WithNegativeTokenCount_ThrowsArgumentException()
    {
        // Arrange
        var embedding = Vector<double>.Build.DenseOfArray(new double[] { 1, 0, 0 });

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ContextItem("content", embedding, -1));
    }

    [Fact]
    public async Task AddMessageAsync_WithMultimodalContent_HandlesCorrectly()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var message = new ChatMessage(ChatRole.User, new List<AIContent>
        {
            new TextContent("What's in this image?"),
            new TextContent("Please describe it.")
        });

        // Act
        await renderer.AddMessageAsync(message);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Content.Should().Contain("What's in this image?");
        renderer.Items[0].Content.Should().Contain("Please describe it.");
    }

    [Fact]
    public async Task AddMessageAsync_WithSystemRole_HandlesCorrectly()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var message = new ChatMessage(ChatRole.System, "You are a helpful assistant.");

        // Act
        await renderer.AddMessageAsync(message);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Content.Should().Contain("system");
        renderer.Items[0].Content.Should().Contain("helpful assistant");
    }
}
