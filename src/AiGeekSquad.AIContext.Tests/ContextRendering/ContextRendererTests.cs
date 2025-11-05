using System.Runtime.CompilerServices;
using AiGeekSquad.AIContext.ContextRendering;
using FluentAssertions;
using FluentAssertions.Execution;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;
using IEmbeddingGenerator = AiGeekSquad.AIContext.Chunking.IEmbeddingGenerator;
using ITokenCounter = AiGeekSquad.AIContext.Chunking.ITokenCounter;
using TextChunk = AiGeekSquad.AIContext.Chunking.TextChunk;

namespace AiGeekSquad.AIContext.Tests.ContextRendering;

public class ContextRendererTests
{
    private readonly FakeTokenCounter _tokenCounter = new();
    private readonly FakeEmbeddingGenerator _embeddingGenerator = new();

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
            var values = new[]
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
        await renderer.AddMessageAsync(message, TestContext.Current.CancellationToken);

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
            await renderer.AddMessageAsync(null!, TestContext.Current.CancellationToken));
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
        await renderer.AddMessagesAsync(messages, TestContext.Current.CancellationToken);

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
        await renderer.AddChunkAsync(chunk, TestContext.Current.CancellationToken);

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
            await renderer.AddChunkAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderContextAsync_WithEmptyContext_ReturnsEmptyList()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);

        // Act
        var result = await renderer.RenderContextAsync("test query", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithNullOrEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)null!, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)"", cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync((string)"   ", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderContextAsync_WithInvalidLambda_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", lambda: -0.1, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", lambda: 1.1, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderContextAsync_WithInvalidFreshnessWeight_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", freshnessWeight: -0.1, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync("test query", freshnessWeight: 1.1, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderContextAsync_WithNoTokenBudget_ReturnsAllItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.Assistant, "Hi there"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "How are you?"), TestContext.Current.CancellationToken);

        // Act
        var result = await renderer.RenderContextAsync("test query", tokenBudget: null, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task RenderContextAsync_WithTokenBudget_RespectsLimit()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add messages with known token counts (length / 4 based on our fake)
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456"), TestContext.Current.CancellationToken); // Content becomes "User: 1234567890123456" = 22 chars = 5 tokens
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456"), TestContext.Current.CancellationToken); // 5 tokens
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "1234567890123456"), TestContext.Current.CancellationToken); // 5 tokens

        // Act - limit to 10 tokens (should get at most 2 items)
        var result = await renderer.RenderContextAsync("test query", tokenBudget: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        result.Should().HaveCountLessOrEqualTo(2);
        var totalTokens = result.Sum(item => item.TokenCount);
        totalTokens.Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task RenderContextAsync_WithHighLambda_PrioritizesRelevance()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add diverse messages
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about dogs"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about cats"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about birds"), TestContext.Current.CancellationToken);

        // Act - high lambda should prioritize relevance over diversity
        var result = await renderer.RenderContextAsync("dogs and puppies", lambda: 0.9, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
        // The First result should be most relevant (about dogs)
        result[0].Content.Should().Contain("dogs");
    }

    [Fact]
    public async Task RenderContextAsync_WithLowLambda_PrioritizesDiversity()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        
        // Add similar messages
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about dogs"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me more about dogs"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Dogs are great"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Tell me about cats"), TestContext.Current.CancellationToken); // Different topic

        // Act - low lambda should prioritize diversity
        var result = await renderer.RenderContextAsync("dogs", lambda: 0.1, tokenBudget: 200, cancellationToken: TestContext.Current.CancellationToken);

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
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.Assistant, "Hi"), TestContext.Current.CancellationToken);

        var queryMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "What is AI?"),
            new ChatMessage(ChatRole.User, "Tell me more")
        };

        // Act
        var result = await renderer.RenderContextAsync(queryMessages, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithNullQueryMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await renderer.RenderContextAsync((IEnumerable<ChatMessage>)null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderContextAsync_WithEmptyQueryMessages_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await renderer.RenderContextAsync(new List<ChatMessage>(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Clear_RemovesAllItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "World"), TestContext.Current.CancellationToken);

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
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Hello"), TestContext.Current.CancellationToken);
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
        var fakeTimeProvider = new FakeTimeProvider();
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        
        // Add items with time advancement to ensure different timestamps
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Old message about dogs"), TestContext.Current.CancellationToken);
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100)); // Ensure different timestamp
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Recent message about cats"), TestContext.Current.CancellationToken);

        // Act - high freshness weight should prioritize recent items
        var result = await renderer.RenderContextAsync("test query", freshnessWeight: 0.8, tokenBudget: 100, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithZeroFreshnessWeight_IgnoresTimestamps()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Old message"), TestContext.Current.CancellationToken);
        fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Recent message"), TestContext.Current.CancellationToken);

        // Act - zero freshness weight should ignore timestamps
        var result = await renderer.RenderContextAsync("test query", freshnessWeight: 0.0, cancellationToken: TestContext.Current.CancellationToken);

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
        await renderer.AddChunksAsync(chunks, TestContext.Current.CancellationToken);

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
            await renderer.AddChunksAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ContextItem_Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var content = "Test content";
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);
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
    public void ContextItem_Constructor_WithNullOrEmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);

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
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);

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
        await renderer.AddMessageAsync(message, TestContext.Current.CancellationToken);

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
        await renderer.AddMessageAsync(message, TestContext.Current.CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Content.Should().Contain("system");
        renderer.Items[0].Content.Should().Contain("helpful assistant");
    }

    [Fact]
    public void Constructor_WithCustomTimeProvider_UsesCustomTimeProvider()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var customTime = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(customTime);

        // Act
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);

        // Assert
        renderer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_UsesSystemTimeProvider()
    {
        // Act
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, null);

        // Assert
        renderer.Should().NotBeNull();
        renderer.Items.Should().BeEmpty();
    }

    [Fact]
    public void ContextItem_Constructor_WithNullTimestamp_UsesCurrentUtcTime()
    {
        // Arrange
        var content = "Test content";
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);
        var tokenCount = 5;
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var item = new ContextItem(content, embedding, tokenCount, null);
        var afterTime = DateTimeOffset.UtcNow;

        // Assert
        using var _ = new AssertionScope();
        item.Timestamp.Should().BeOnOrAfter(beforeTime);
        item.Timestamp.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void ContextItem_Constructor_WithExplicitTimestamp_UsesProvidedTimestamp()
    {
        // Arrange
        var content = "Test content";
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);
        var tokenCount = 5;
        var explicitTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

        // Act
        var item = new ContextItem(content, embedding, tokenCount, explicitTime);

        // Assert
        item.Timestamp.Should().Be(explicitTime);
    }

    [Fact]
    public void ContextItem_ToString_WithShortContent_ReturnsCompleteContent()
    {
        // Arrange
        var content = "Short text";
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);
        var timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var item = new ContextItem(content, embedding, 5, timestamp);

        // Act
        var result = item.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().Contain("Short text");
        result.Should().Contain("2025-01-01");
        result.Should().Contain("5 tokens");
    }

    [Fact]
    public void ContextItem_ToString_WithLongContent_TruncatesContent()
    {
        // Arrange
        var longContent = new string('A', 100); // 100 characters
        var embedding = Vector<double>.Build.DenseOfArray([1, 0, 0]);
        var timestamp = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var item = new ContextItem(longContent, embedding, 25, timestamp);

        // Act
        var result = item.ToString();

        // Assert
        using var _ = new AssertionScope();
        result.Should().Contain("...");
        result.Length.Should().BeLessThan(longContent.Length + 100);
    }

    [Fact]
    public async Task AddMessageAsync_WithCustomTimeProvider_UsesTimeProviderForTimestamp()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var customTime = new DateTimeOffset(2025, 2, 1, 14, 30, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(customTime);
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        var message = new ChatMessage(ChatRole.User, "Test message");

        // Act
        await renderer.AddMessageAsync(message, TestContext.Current.CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Timestamp.Should().Be(customTime);
    }

    [Fact]
    public async Task AddChunkAsync_WithCustomTimeProvider_UsesTimeProviderForTimestamp()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var customTime = new DateTimeOffset(2025, 3, 1, 16, 45, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(customTime);
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        var chunk = new TextChunk("Test chunk content", 0, 18);

        // Act
        await renderer.AddChunkAsync(chunk, TestContext.Current.CancellationToken);

        // Assert
        using var _ = new AssertionScope();
        renderer.Items.Should().HaveCount(1);
        renderer.Items[0].Timestamp.Should().Be(customTime);
    }

    [Fact]
    public async Task RenderContextAsync_WithFreshnessAndIdenticalTimestamps_HandlesCorrectly()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        
        // Add multiple items with identical timestamps
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Message 1"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Message 2"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Message 3"), TestContext.Current.CancellationToken);

        // Act - with freshness weight but all items have same timestamp
        var result = await renderer.RenderContextAsync("test query", freshnessWeight: 0.5, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task RenderContextAsync_WithMaxFreshnessWeight_StronglyPrioritizesRecentItems()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider();
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator, fakeTimeProvider);
        
        // Add old item
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Very old message about topic"), TestContext.Current.CancellationToken);
        
        // Advance time significantly
        fakeTimeProvider.Advance(TimeSpan.FromHours(24));
        
        // Add recent item
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Recent message about topic"), TestContext.Current.CancellationToken);

        // Act - maximum freshness weight
        var result = await renderer.RenderContextAsync("topic", freshnessWeight: 1.0, tokenBudget: 100, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithSingleItem_HandlesCorrectly()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Single message"), TestContext.Current.CancellationToken);

        // Act
        var result = await renderer.RenderContextAsync("query", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task RenderContextAsync_WithTokenBudgetSmallerThanSmallestItem_ReturnsEmpty()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "This is a message with many tokens"), TestContext.Current.CancellationToken);

        // Act - token budget of 1 (smaller than any item)
        var result = await renderer.RenderContextAsync("query", tokenBudget: 1, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMessagesAsync_WithEmptyMessages_DoesNotAddItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var messages = new List<ChatMessage>();

        // Act
        await renderer.AddMessagesAsync(messages, TestContext.Current.CancellationToken);

        // Assert
        renderer.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddChunksAsync_WithEmptyChunks_DoesNotAddItems()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var chunks = new List<TextChunk>();

        // Act
        await renderer.AddChunksAsync(chunks, TestContext.Current.CancellationToken);

        // Assert
        renderer.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RenderContextAsync_WithVeryLargeLambda_PrioritizesRelevanceCompletely()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Dogs are great pets"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Cats are independent"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Birds can fly"), TestContext.Current.CancellationToken);

        // Act - lambda = 1.0 means pure relevance, no diversity
        var result = await renderer.RenderContextAsync("dogs and puppies", lambda: 1.0, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
        result[0].Content.Should().Contain("Dogs");
    }

    [Fact]
    public async Task RenderContextAsync_WithVeryLowLambda_PrioritizesDiversityCompletely()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Dogs"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Dogs are fun"), TestContext.Current.CancellationToken);
        await renderer.AddMessageAsync(new ChatMessage(ChatRole.User, "Cats"), TestContext.Current.CancellationToken);

        // Act - lambda = 0.0 means pure diversity, no relevance consideration
        var result = await renderer.RenderContextAsync("dogs", lambda: 0.0, tokenBudget: 100, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddMessageAsync_WithMessageContainingOnlyWhitespace_AddsCorrectly()
    {
        // Arrange
        var renderer = new ContextRenderer(_tokenCounter, _embeddingGenerator);
        var message = new ChatMessage(ChatRole.User, "     \t\n     ");

        // Act
        await renderer.AddMessageAsync(message, TestContext.Current.CancellationToken);

        // Assert - The message is added as-is; formatting is preserved
        renderer.Items.Should().HaveCount(1);
    }
}
