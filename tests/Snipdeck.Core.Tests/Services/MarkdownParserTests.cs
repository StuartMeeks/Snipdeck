using Snipdeck.Core.Services.Markdown;

namespace Snipdeck.Core.Tests.Services
{
    public class MarkdownParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_input_yields_no_blocks(string? input)
        {
            Assert.Empty(MarkdownParser.Parse(input));
        }

        [Fact]
        public void Plain_paragraph_becomes_a_single_text_run()
        {
            var blocks = MarkdownParser.Parse("Just some text.");

            var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(blocks));
            var run = Assert.IsType<TextRun>(Assert.Single(paragraph.Inlines));
            Assert.Equal("Just some text.", run.Text);
            Assert.False(run.Bold);
            Assert.False(run.Italic);
            Assert.False(run.Code);
        }

        [Fact]
        public void Bold_italic_and_inline_code_set_the_run_flags()
        {
            var blocks = MarkdownParser.Parse("**b** *i* `c`");
            var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(blocks));

            var bold = Assert.IsType<TextRun>(paragraph.Inlines[0]);
            Assert.Equal("b", bold.Text);
            Assert.True(bold.Bold);

            var italic = paragraph.Inlines.OfType<TextRun>().Single(r => r.Text == "i");
            Assert.True(italic.Italic);

            var code = paragraph.Inlines.OfType<TextRun>().Single(r => r.Text == "c");
            Assert.True(code.Code);
        }

        [Fact]
        public void Heading_captures_level_and_text()
        {
            var heading = Assert.IsType<HeadingBlock>(Assert.Single(MarkdownParser.Parse("### Usage")));
            Assert.Equal(3, heading.Level);
            Assert.Equal("Usage", Assert.IsType<TextRun>(Assert.Single(heading.Inlines)).Text);
        }

        [Fact]
        public void Fenced_code_block_is_captured_verbatim()
        {
            var blocks = MarkdownParser.Parse("```\npl-app deploy --env prod\n```");

            var code = Assert.IsType<CodeBlock>(Assert.Single(blocks));
            Assert.Equal("pl-app deploy --env prod", code.Text);
        }

        [Fact]
        public void Unordered_list_yields_items()
        {
            var list = Assert.IsType<ListBlock>(Assert.Single(MarkdownParser.Parse("- first\n- second")));
            Assert.False(list.Ordered);
            Assert.Equal(2, list.Items.Count);
            Assert.Equal("first", Assert.IsType<TextRun>(Assert.Single(list.Items[0].Inlines)).Text);
            Assert.Equal("second", Assert.IsType<TextRun>(Assert.Single(list.Items[1].Inlines)).Text);
        }

        [Fact]
        public void Ordered_list_is_flagged_ordered()
        {
            var list = Assert.IsType<ListBlock>(Assert.Single(MarkdownParser.Parse("1. one\n2. two")));
            Assert.True(list.Ordered);
            Assert.Equal(2, list.Items.Count);
        }

        [Fact]
        public void Link_captures_text_and_url()
        {
            var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(MarkdownParser.Parse("See [the docs](https://example.com/x).")));
            var link = paragraph.Inlines.OfType<LinkRun>().Single();
            Assert.Equal("the docs", link.Text);
            Assert.Equal("https://example.com/x", link.Url);
        }

        [Fact]
        public void Hard_line_break_becomes_a_line_break_run()
        {
            // Two trailing spaces before the newline is a markdown hard break.
            var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(MarkdownParser.Parse("line one  \nline two")));
            Assert.Contains(paragraph.Inlines, i => i is LineBreakRun);
        }
    }
}
