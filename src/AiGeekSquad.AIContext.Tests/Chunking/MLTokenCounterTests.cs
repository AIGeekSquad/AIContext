using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class MLTokenCounterTests
    {
        [Fact]
        public void Constructor_WithNullTokenizer_CreatesDefaultTokenizer()
        {
            // Act
            var tokenCounter = new MLTokenCounter();

            // Assert
            tokenCounter.Should().NotBeNull();
        }

        [Fact]
        public void CountTokens_WithNullText_ThrowsArgumentNullException()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act & Assert
            var act = () => tokenCounter.CountTokens(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CountTokens_WithEmptyText_ReturnsZero()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act
            var result = tokenCounter.CountTokens("");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CountTokens_WithSimpleText_ReturnsPositiveCount()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "Hello world!";

            // Act
            var result = tokenCounter.CountTokens(text);

            // Assert
            using var _ = new AssertionScope();
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThanOrEqualTo(10); // Simple text shouldn't have too many tokens
        }

        [Fact]
        public async Task CountTokensAsync_WithNullText_ThrowsArgumentNullException()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act & Assert
            var act = async () => await tokenCounter.CountTokensAsync(null!);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CountTokensAsync_WithEmptyText_ReturnsZero()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act
            var result = await tokenCounter.CountTokensAsync("");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task CountTokensAsync_WithSimpleText_ReturnsPositiveCount()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "Hello world!";

            // Act
            var result = await tokenCounter.CountTokensAsync(text);

            // Assert
            using var _ = new AssertionScope();
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThanOrEqualTo(10);
        }

        [Fact]
        public async Task CountTokensAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "Hello world!";
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            // Act & Assert
            var act = async () => await tokenCounter.CountTokensAsync(text, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void CountTokens_SameText_ReturnsSameCount()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "This is a test sentence with multiple words.";

            // Act
            var count1 = tokenCounter.CountTokens(text);
            var count2 = tokenCounter.CountTokens(text);

            // Assert
            using var _ = new AssertionScope();
            count1.Should().Be(count2);
            count1.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateGpt4_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateGpt4();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateGpt35Turbo_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateGpt35Turbo();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateTextEmbeddingAda002_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateTextEmbeddingAda002();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateTextEmbedding3Small_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateTextEmbedding3Small();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateTextEmbedding3Large_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateTextEmbedding3Large();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateCl100kBase_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateCl100kBase();

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateForModel_WithValidModel_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateForModel("gpt-4");

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateForModel_WithNullModel_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForModel(null!);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForModel_WithEmptyModel_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForModel("");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForModel_WithWhitespaceModel_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForModel("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForModel_WithInvalidModel_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForModel("invalid-model-name-123");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void CreateForEncoding_WithValidEncoding_CreatesValidTokenCounter()
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateForEncoding("cl100k_base");

            // Assert
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateForEncoding_WithNullEncoding_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForEncoding(null!);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForEncoding_WithEmptyEncoding_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForEncoding("");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForEncoding_WithWhitespaceEncoding_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForEncoding("   ");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateForEncoding_WithInvalidEncoding_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var act = () => MLTokenCounter.CreateForEncoding("invalid-encoding-name");
            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData("gpt-4")]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("text-embedding-ada-002")]
        [InlineData("text-embedding-3-small")]
        [InlineData("text-embedding-3-large")]
        public void CreateForModel_WithVariousValidModels_CreatesValidTokenCounters(string modelName)
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateForModel(modelName);

            // Assert
            using var _ = new AssertionScope();
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThanOrEqualTo(10); // Simple text shouldn't have too many tokens
        }

        [Theory]
        [InlineData("cl100k_base")]
        public void CreateForEncoding_WithVariousValidEncodings_CreatesValidTokenCounters(string encodingName)
        {
            // Act
            var tokenCounter = MLTokenCounter.CreateForEncoding(encodingName);

            // Assert
            using var _ = new AssertionScope();
            tokenCounter.Should().NotBeNull();

            // Test that it can count tokens
            var result = tokenCounter.CountTokens("Hello world!");
            result.Should().BeGreaterThan(0);
            result.Should().BeLessThanOrEqualTo(10); // Simple text shouldn't have too many tokens
        }

        [Fact]
        public void DifferentTokenizers_OnSameText_MayProduceDifferentCounts()
        {
            // Arrange
            var text = "This is a longer text sample to test tokenization differences between models.";
            var gpt4Counter = MLTokenCounter.CreateGpt4();
            var cl100kCounter = MLTokenCounter.CreateCl100kBase();

            // Act
            var gpt4Count = gpt4Counter.CountTokens(text);
            var cl100kCount = cl100kCounter.CountTokens(text);

            // Assert
            using var _ = new AssertionScope();
            gpt4Count.Should().BeGreaterThan(0);
            cl100kCount.Should().BeGreaterThan(0);

            // Note: GPT-4 and cl100k_base actually use the same encoding, so counts should be equal
            // This test verifies the tokenizers work correctly
            gpt4Count.Should().Be(cl100kCount);
        }

        [Fact]
        public async Task CountTokensAsync_SameResultAsSync()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "This is a test sentence for comparing sync and async token counting.";

            // Act
            var syncCount = tokenCounter.CountTokens(text);
            var asyncCount = await tokenCounter.CountTokensAsync(text);

            // Assert
            using var _ = new AssertionScope();
            syncCount.Should().Be(asyncCount);
            syncCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();

            // Act & Assert
            var act = () => tokenCounter.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void TokenCounter_AfterDispose_StillWorks()
        {
            // Arrange
            var tokenCounter = new MLTokenCounter();
            var text = "Hello world!";

            // Act
            tokenCounter.Dispose();
            var result = tokenCounter.CountTokens(text);

            // Assert
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void MultipleFactoryMethods_CreateIndependentInstances()
        {
            // Act
            var counter1 = MLTokenCounter.CreateGpt4();
            var counter2 = MLTokenCounter.CreateGpt4();

            // Assert
            using var _ = new AssertionScope();
            counter1.Should().NotBeSameAs(counter2);

            // Both should work independently
            var text = "Test text";
            var count1 = counter1.CountTokens(text);
            var count2 = counter2.CountTokens(text);


            count1.Should().Be(count2);
            count1.Should().BeGreaterThan(0);
        }
    }
}
