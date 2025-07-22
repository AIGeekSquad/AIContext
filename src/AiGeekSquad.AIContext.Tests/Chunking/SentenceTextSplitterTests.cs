using AiGeekSquad.AIContext.Chunking;

using FluentAssertions;
using FluentAssertions.Execution;

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
                var extractedText = text[segment.StartIndex..segment.EndIndex];
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
        public async Task SplitAsync_WithDoctorTitlesAndAbbreviations_DefaultPatternDoesNotSplitOnAbbreviations()
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
            // The updated default pattern now avoids splitting on common abbreviations
            segments.Should().HaveCount(3);
            // Verify that abbreviations are NOT split (new behavior with improved pattern)
            segments[0].Text.Should().Be("Dr. Smith works at the hospital.");
            segments[1].Text.Should().Be("Prof. Johnson teaches at the university.");
            segments[2].Text.Should().Be("Mr. Brown and Mrs. Green are attending the meeting.");
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

        [Fact]
        public async Task SplitAsync_WithQuotedText_HandlesQuotedSentences()
        {
            // Arrange
            var splitter = new SentenceTextSplitter();
            var text = "He said, \"How do you draw an Owl Mr. Crawley ?\" to Dr. Tom. No one answered.";

            // Act
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
            {
                segments.Add(segment);
            }

            // Assert
            using var _ = new AssertionScope();
            
 
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("He said, \"How do you draw an Owl Mr. Crawley ?\" to Dr. Tom.");
            segments[1].Text.Should().Be("No one answered.");
        
        }
        // MARKDOWN TESTS

        [Fact]
        public async Task SplitAsync_Markdown_UnorderedLists_AllBulletTypes()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- Item one\n* Item two\n+ Item three";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- Item one");
            segments[1].Text.Should().Be("* Item two");
            segments[2].Text.Should().Be("+ Item three");
        }

        [Fact]
        public async Task SplitAsync_Markdown_OrderedLists_Numbers()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "1. First item\n2. Second item\n3. Third item";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("1. First item");
            segments[1].Text.Should().Be("2. Second item");
            segments[2].Text.Should().Be("3. Third item");
        }

        [Fact]
        public async Task SplitAsync_Markdown_NestedLists()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- Parent\n  - Child 1\n  - Child 2\n    * Grandchild";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("- Parent");
            segments[1].Text.Should().Be("  - Child 1");
            segments[2].Text.Should().Be("  - Child 2");
            segments[3].Text.Should().Be("    * Grandchild");
        }

        [Fact]
        public async Task SplitAsync_Markdown_ListItemsWithMultipleSentences()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- First item. Has two sentences!\n- Second item? Yes.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("- First item. Has two sentences!");
            segments[1].Text.Should().Be("- Second item? Yes.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedListsAndParagraphs()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- List item\n\nParagraph one. Paragraph two!\n- Another item";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("- List item");
            segments[1].Text.Should().Be("Paragraph one.");
            segments[2].Text.Should().Be("Paragraph two!");
            segments[3].Text.Should().Be("- Another item");
        }

        [Fact]
        public async Task SplitAsync_Markdown_Headers()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "# Header 1\n## Header 2\n### Header 3";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("# Header 1");
            segments[1].Text.Should().Be("## Header 2");
            segments[2].Text.Should().Be("### Header 3");
        }

        [Fact]
        public async Task SplitAsync_Markdown_CodeBlocks_FencedAndIndented()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "```\ncode block\n```\n    indented code";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("```\ncode block\n```");
            segments[1].Text.Should().Be("    indented code");
        }

        [Fact]
        public async Task SplitAsync_Markdown_InlineCode()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "This is `inline code` in a sentence.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("This is `inline code` in a sentence.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_LinksAndImages()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "[Link](https://example.com) and ![Image](img.png)";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            segments.Should().HaveCount(1);
            segments[0].Text.Should().Be("[Link](https://example.com) and ![Image](img.png)");
        }

        [Fact]
        public async Task SplitAsync_Markdown_Blockquotes()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "> This is a blockquote.\n> Second line.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(2);
            segments[0].Text.Should().Be("> This is a blockquote.");
            segments[1].Text.Should().Be("> Second line.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedDocument()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "# Title\n- List item\nParagraph one. `inline code`\n```\nblock\n```\n[Link](url)";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(6);
            segments[0].Text.Should().Be("# Title");
            segments[1].Text.Should().Be("- List item");
            segments[2].Text.Should().Be("Paragraph one.");
            segments[3].Text.Should().Be("`inline code`");
            segments[4].Text.Should().Be("```\nblock\n```");
            segments[5].Text.Should().Be("[Link](url)");
        }

        [Fact]
        public async Task SplitAsync_Markdown_EmptyElements()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "- \n\n```\n\n```\n# \n";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- ");
            segments[1].Text.Should().Be("```\n\n```");
            segments[2].Text.Should().Be("# ");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MalformedMarkdown()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "-Item without space\n*Another\n1.Item\n##HeaderNoSpace";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(4);
            segments[0].Text.Should().Be("-Item without space");
            segments[1].Text.Should().Be("*Another");
            segments[2].Text.Should().Be("1.Item");
            segments[3].Text.Should().Be("##HeaderNoSpace");
        }

        [Fact]
        public async Task SplitAsync_Markdown_MixedWithRegularText()
        {
            var splitter = SentenceTextSplitter.ForMarkdown();
            var text = "This is a paragraph. - List item\nAnother sentence!";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("This is a paragraph.");
            segments[1].Text.Should().Be("- List item");
            segments[2].Text.Should().Be("Another sentence!");
        }

        [Fact]
        public async Task SplitAsync_Markdown_WithPatternForMarkdown_Works()
        {
            var splitter = SentenceTextSplitter.WithPatternForMarkdown(@"(?<=\n)");
            var text = "# Header\n- List\nParagraph.";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("# Header");
            segments[1].Text.Should().Be("- List");
            segments[2].Text.Should().Be("Paragraph.");
        }

        [Fact]
        public async Task SplitAsync_Markdown_BackwardCompatibility_NonMarkdownMode()
        {
            var splitter = new SentenceTextSplitter();
            var text = "- List item. Paragraph one. Paragraph two!";
            var segments = new List<TextSegment>();
            await foreach (var segment in splitter.SplitAsync(text))
                segments.Add(segment);

            using var _ = new AssertionScope();
            // Should split only by sentences, not preserve markdown
            segments.Should().HaveCount(3);
            segments[0].Text.Should().Be("- List item.");
            segments[1].Text.Should().Be("Paragraph one.");
            segments[2].Text.Should().Be("Paragraph two!");
        }
    }
}
