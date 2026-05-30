namespace Snipdeck.Core.Services.Markdown
{
    /// <summary>
    /// A small, UI-free representation of rendered markdown. The Core parser
    /// produces this model from markdown source; a UI head maps it onto its own
    /// native text primitives (WinUI inlines, etc.). Keeping the model here lets
    /// the parsing be unit-tested without any UI dependency.
    /// </summary>
    public abstract record MarkdownBlock;

    /// <summary>A run of body text made up of styled inline spans.</summary>
    public sealed record ParagraphBlock(IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

    /// <summary>A heading (<paramref name="Level"/> 1-6) made up of inline spans.</summary>
    public sealed record HeadingBlock(int Level, IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

    /// <summary>A fenced or indented code block, rendered verbatim in a monospace box.</summary>
    public sealed record CodeBlock(string Text) : MarkdownBlock;

    /// <summary>An ordered or unordered list. Items are flattened to inline content.</summary>
    public sealed record ListBlock(bool Ordered, IReadOnlyList<ListItem> Items) : MarkdownBlock;

    /// <summary>A single list item's inline content.</summary>
    public sealed record ListItem(IReadOnlyList<MarkdownInline> Inlines);

    /// <summary>An inline span within a block.</summary>
    public abstract record MarkdownInline;

    /// <summary>A span of text carrying emphasis flags. <paramref name="Code"/> marks inline code.</summary>
    public sealed record TextRun(string Text, bool Bold = false, bool Italic = false, bool Code = false) : MarkdownInline;

    /// <summary>A hyperlink span.</summary>
    public sealed record LinkRun(string Text, string Url, bool Bold = false, bool Italic = false) : MarkdownInline;

    /// <summary>A hard line break within a paragraph.</summary>
    public sealed record LineBreakRun : MarkdownInline;
}
