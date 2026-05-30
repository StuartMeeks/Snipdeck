using System.Text;

using Markdig;

using MdInlines = Markdig.Syntax.Inlines;
using MdSyntax = Markdig.Syntax;

namespace Snipdeck.Core.Services.Markdown
{
    /// <summary>
    /// Parses markdown source into the UI-free <see cref="MarkdownBlock"/> model
    /// using Markdig. A plain pipeline (no advanced extensions) keeps the output
    /// predictable: paragraphs, headings, emphasis, inline/block code, links and
    /// simple lists — the subset relevant to short snip descriptions.
    /// </summary>
    public static class MarkdownParser
    {
        private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder().Build();

        public static IReadOnlyList<MarkdownBlock> Parse(string? markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return [];
            }

            var document = Markdig.Markdown.Parse(markdown, _pipeline);
            var blocks = new List<MarkdownBlock>();
            foreach (var block in document)
            {
                AppendBlock(block, blocks);
            }
            return blocks;
        }

        private static void AppendBlock(MdSyntax.Block block, List<MarkdownBlock> output)
        {
            switch (block)
            {
                case MdSyntax.HeadingBlock heading:
                    output.Add(new HeadingBlock(Math.Clamp(heading.Level, 1, 6), ParseInlines(heading.Inline)));
                    break;
                case MdSyntax.ParagraphBlock paragraph:
                    output.Add(new ParagraphBlock(ParseInlines(paragraph.Inline)));
                    break;
                // FencedCodeBlock derives from CodeBlock, so it must be matched first.
                case MdSyntax.FencedCodeBlock fenced:
                    output.Add(new CodeBlock(fenced.Lines.ToString().TrimEnd('\r', '\n')));
                    break;
                case MdSyntax.CodeBlock code:
                    output.Add(new CodeBlock(code.Lines.ToString().TrimEnd('\r', '\n')));
                    break;
                case MdSyntax.ListBlock list:
                    {
                        var items = new List<ListItem>();
                        foreach (var child in list)
                        {
                            if (child is MdSyntax.ListItemBlock listItem)
                            {
                                items.Add(new ListItem(ParseListItemInlines(listItem)));
                            }
                        }
                        output.Add(new ListBlock(list.IsOrdered, items));
                        break;
                    }
                // QuoteBlock and any other container: flatten its children. Both
                // derive from ContainerBlock, so this arm must come last.
                case MdSyntax.ContainerBlock container:
                    foreach (var child in container)
                    {
                        AppendBlock(child, output);
                    }
                    break;
                default:
                    // Thematic breaks, raw HTML, etc. have no place in a snip
                    // description render — drop them silently.
                    break;
            }
        }

        private static List<MarkdownInline> ParseListItemInlines(MdSyntax.ListItemBlock listItem)
        {
            var inlines = new List<MarkdownInline>();
            foreach (var child in listItem)
            {
                if (child is MdSyntax.ParagraphBlock paragraph)
                {
                    if (inlines.Count > 0)
                    {
                        inlines.Add(new LineBreakRun());
                    }
                    inlines.AddRange(ParseInlines(paragraph.Inline));
                }
            }
            return inlines;
        }

        private static List<MarkdownInline> ParseInlines(MdInlines.ContainerInline? container)
        {
            var result = new List<MarkdownInline>();
            if (container is not null)
            {
                WalkInlines(container, bold: false, italic: false, result);
            }
            return result;
        }

        private static void WalkInlines(MdInlines.ContainerInline container, bool bold, bool italic, List<MarkdownInline> output)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case MdInlines.LiteralInline literal:
                        {
                            var text = literal.Content.ToString();
                            if (text.Length > 0)
                            {
                                output.Add(new TextRun(text, bold, italic));
                            }
                            break;
                        }
                    case MdInlines.CodeInline code:
                        output.Add(new TextRun(code.Content, bold, italic, Code: true));
                        break;
                    case MdInlines.EmphasisInline emphasis:
                        {
                            // One delimiter (*x* / _x_) is italic; two (**x**) is bold.
                            var nestedBold = emphasis.DelimiterCount >= 2 || bold;
                            var nestedItalic = emphasis.DelimiterCount == 1 || italic;
                            WalkInlines(emphasis, nestedBold, nestedItalic, output);
                            break;
                        }
                    case MdInlines.LinkInline { IsImage: false } link:
                        output.Add(new LinkRun(CollectText(link), link.Url ?? string.Empty, bold, italic));
                        break;
                    case MdInlines.LinkInline image:
                        {
                            // No image rendering — fall back to the alt text.
                            var alt = CollectText(image);
                            if (alt.Length > 0)
                            {
                                output.Add(new TextRun(alt, bold, italic));
                            }
                            break;
                        }
                    case MdInlines.LineBreakInline lineBreak:
                        // A hard break is an explicit newline; a soft break (a plain
                        // newline in the source) just keeps words separated.
                        output.Add(lineBreak.IsHard ? new LineBreakRun() : new TextRun(" ", bold, italic));
                        break;
                    case MdInlines.ContainerInline nested:
                        WalkInlines(nested, bold, italic, output);
                        break;
                    default:
                        // Autolinks, raw inline HTML, etc. — ignored for this subset.
                        break;
                }
            }
        }

        private static string CollectText(MdInlines.ContainerInline container)
        {
            var builder = new StringBuilder();
            CollectText(container, builder);
            return builder.ToString();
        }

        private static void CollectText(MdInlines.ContainerInline container, StringBuilder builder)
        {
            foreach (var inline in container)
            {
                switch (inline)
                {
                    case MdInlines.LiteralInline literal:
                        _ = builder.Append(literal.Content.ToString());
                        break;
                    case MdInlines.CodeInline code:
                        _ = builder.Append(code.Content);
                        break;
                    case MdInlines.ContainerInline nested:
                        CollectText(nested, builder);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
