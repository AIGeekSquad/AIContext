using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AiGeekSquad.AIContext.Tests.Chunking
{
    public class SentenceTextSplitterTests
    {
        [Fact]
        public async Task SplitAsync_WithSimpleSentences_SplitsCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is the first sentence. This is the second sentence! This is the third sentence?";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("This is the first sentence.");
            segments[1].Text.Should().Be("This is the second sentence!");
            segments[2].Text.Should().Be("This is the third sentence?");
            segments[0].StartIndex.Should().Be(0);
            segments[0].EndIndex.Should().Be(27);
        }

        [Fact]
        public async Task SplitAsync_WithNullText_ThrowsArgumentNullException()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();

            // Act & Assert
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(null!))
                {
                    // This should not execute
                }
            };

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SplitAsync_WithEmptyText_ReturnsEmptyResult()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            segments.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitAsync_WithWhitespaceText_ReturnsEmptyResult()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "   \t\n\r   ";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            segments.Should().BeEmpty();
        }

        [Fact]
        public async Task SplitAsync_WithSingleSentence_ReturnsSingleSegment()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "This is a single sentence without ending punctuation";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().ContainSingle();
            segments[0].Text.Should().Be("This is a single sentence without ending punctuation");
            segments[0].StartIndex.Should().Be(0);
            segments[0].EndIndex.Should().Be(text.Length);
        }

        [Fact]
        public async Task SplitAsync_WithNoProperSentenceBoundaries_ReturnsSingleSegment()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "this is all lowercase. no capital letters after periods.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().ContainSingle();
            segments[0].Text.Should().Be("this is all lowercase. no capital letters after periods.");
        }

        [Fact]
        public async Task SplitAsync_WithMixedPunctuation_SplitsOnAllTypes()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Question one? Statement two. Exclamation three! Another statement.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("Question one?");
            segments[1].Text.Should().Be("Statement two.");
            segments[2].Text.Should().Be("Exclamation three!");
            segments[3].Text.Should().Be("Another statement.");
        }

        [Fact]
        public async Task SplitAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence. Second sentence. Third sentence.";
            
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            var act = async () =>
            {
                await foreach (var segment in splitter.SplitAsync(text, cts.Token))
                {
                    // This should not execute due to cancellation
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task SplitAsync_WithExtraWhitespace_TrimsCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "  First sentence.   Second sentence.  ";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("First sentence.");
            segments[1].Text.Should().Be("Second sentence.");
            // Should maintain original positions in text
            segments[0].StartIndex.Should().Be(2);
            segments[1].StartIndex.Should().BeGreaterThan(segments[0].EndIndex);
        }

        [Fact]
        public void WithPattern_WithValidPattern_CreatesCustomSplitter()
        {
            // Arrange
            var customPattern = @"\s*\|\s*"; // Split on pipe characters

            // Act
            var splitter = SentenceTextSplitter.WithPattern(customPattern);

            // Assert
            splitter.Should().NotBeNull();
            splitter.Should().BeOfType<SentenceTextSplitter>();
        }

        [Fact]
        public void WithPattern_WithNullPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern(null!);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public void WithPattern_WithEmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern("");
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public void WithPattern_WithWhitespacePattern_ThrowsArgumentException()
        {
            // Act & Assert
            var act = () => SentenceTextSplitter.WithPattern("   ");
            act.Should().Throw<ArgumentException>()
                .WithMessage("Pattern cannot be null or empty.*");
        }

        [Fact]
        public async Task SplitAsync_WithCustomPattern_UsesCustomSplitting()
        {
            // Arrange
            var customPattern = @"\s*\|\s*"; // Split on pipe characters
            var splitter = SentenceTextSplitter.WithPattern(customPattern);
            var text = "First part | Second part | Third part";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("First part");
            segments[1].Text.Should().Be("Second part");
            segments[2].Text.Should().Be("Third part");
        }

        [Fact]
        public void Default_ReturnsNewInstanceWithDefaultPattern()
        {
            // Act
            var splitter1 = SentenceTextSplitter.Default;
            var splitter2 = SentenceTextSplitter.Default;

            // Assert
            using var _ = new AssertionScope();
            splitter1.Should().NotBeNull();
            splitter2.Should().NotBeNull();
            splitter1.Should().NotBeSameAs(splitter2); // Should create new instances
        }

        [Fact]
        public async Task SplitAsync_WithComplexText_HandleCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith said hello. He went to the U.S.A. Then he returned! What happened next?";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().NotBeEmpty();
            // Note: Simple regex may not handle abbreviations perfectly, which is expected
            segments.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Text));
        }

        [Fact]
        public async Task SplitAsync_VerifiesTextSegmentProperties()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "First sentence. Second sentence.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            
            foreach (var segment in segments)
            {
                segment.Text.Should().NotBeNullOrWhiteSpace();
                segment.StartIndex.Should().BeGreaterThanOrEqualTo(0);
                segment.EndIndex.Should().BeGreaterThan(segment.StartIndex);
                segment.Length.Should().Be(segment.Text.Length);
                
                // Verify the segment text matches what's in the original text
                var extractedText = text.Substring(segment.StartIndex, segment.EndIndex - segment.StartIndex);
                extractedText.Should().Contain(segment.Text.Trim());
            }
        }

        [Fact]
        public async Task SplitAsync_WithFilenames_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The file config.json contains settings. Please check Program.cs for the implementation. The README.md file has documentation.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The file config.json contains settings.");
            segments[1].Text.Should().Be("Please check Program.cs for the implementation.");
            segments[2].Text.Should().Be("The README.md file has documentation.");
        }

        [Fact]
        public async Task SplitAsync_WithFilePaths_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Open the file at C:\\Users\\john.doe\\Documents\\file.txt for reading. The Linux path /home/user/project/src/main.cpp contains the source code. Check D:\\Projects\\MyApp\\bin\\Debug\\app.exe for the executable.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Open the file at C:\\Users\\john.doe\\Documents\\file.txt for reading.");
            segments[1].Text.Should().Be("The Linux path /home/user/project/src/main.cpp contains the source code.");
            segments[2].Text.Should().Be("Check D:\\Projects\\MyApp\\bin\\Debug\\app.exe for the executable.");
        }

        [Fact]
        public async Task SplitAsync_WithUrls_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Visit https://www.example.com for more information. The API endpoint is https://api.service.com/v1/data. You can also check http://localhost:3000/dashboard for local testing.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Visit https://www.example.com for more information.");
            segments[1].Text.Should().Be("The API endpoint is https://api.service.com/v1/data.");
            segments[2].Text.Should().Be("You can also check http://localhost:3000/dashboard for local testing.");
        }

        [Fact]
        public async Task SplitAsync_WithAcronyms_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The application uses .NET Core framework. We also support ASP.NET and Entity Framework. The U.S.A. is where our main office is located.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The application uses .NET Core framework.");
            segments[1].Text.Should().Be("We also support ASP.NET and Entity Framework.");
            segments[2].Text.Should().Be("The U.S.A. is where our main office is located.");
        }

        [Fact]
        public async Task SplitAsync_WithDoctorTitlesAndAbbreviations_DefaultPatternSplitsOnAbbreviations()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Dr. Smith works at the hospital. Prof. Johnson teaches at the university. Mr. Brown and Mrs. Green are attending the meeting.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            // The default pattern will split on abbreviations followed by capital letters
            segments.Should().HaveCountGreaterThan(3);
            // Verify that abbreviations are split (this is expected behavior with the simple default pattern)
            segments.Should().Contain(s => s.Text == "Dr.");
            segments.Should().Contain(s => s.Text.Contains("Smith works at the hospital."));
        }

        [Fact]
        public async Task SplitAsync_WithCustomPatternForSpecialCases_HandlesCorrectly()
        {
            // Arrange
            // Use double space as sentence delimiter
            var customPattern = @"\s{2,}";
            var splitter = SentenceTextSplitter.WithPattern(customPattern);
            var text = "First sentence here.  Second sentence here.  Third sentence.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("First sentence here.");
            segments[1].Text.Should().Be("Second sentence here.");
            segments[2].Text.Should().Be("Third sentence.");
        }

        [Fact]
        public async Task SplitAsync_WithVersionNumbers_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "The software version is 1.2.3 and it's stable. Please upgrade to version 2.0.1 for better performance. The legacy version 0.9.8 is no longer supported.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("The software version is 1.2.3 and it's stable.");
            segments[1].Text.Should().Be("Please upgrade to version 2.0.1 for better performance.");
            segments[2].Text.Should().Be("The legacy version 0.9.8 is no longer supported.");
        }

        [Fact]
        public async Task SplitAsync_WithComplexTechnicalText_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Install .NET 8.0 from https://dotnet.microsoft.com/download. Configure the appsettings.json file in your project. Run the command dotnet build in the terminal.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Install .NET 8.0 from https://dotnet.microsoft.com/download.");
            segments[1].Text.Should().Be("Configure the appsettings.json file in your project.");
            segments[2].Text.Should().Be("Run the command dotnet build in the terminal.");
        }

        [Fact]
        public async Task SplitAsync_WithEmailAddresses_HandlesCorrectly()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "Contact john.doe@company.com for support. The admin email is admin@system.org and it's monitored daily. For urgent issues, reach out to emergency@service.net immediately.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("Contact john.doe@company.com for support.");
            segments[1].Text.Should().Be("The admin email is admin@system.org and it's monitored daily.");
            segments[2].Text.Should().Be("For urgent issues, reach out to emergency@service.net immediately.");
        }
    }
}