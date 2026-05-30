using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using Snipdeck.Core.Services.Markdown;

namespace Snipdeck.App.Controls
{
    /// <summary>
    /// Renders a markdown string into WinUI text elements. The parsing lives in
    /// Core (<see cref="MarkdownParser"/>); this control only maps the resulting
    /// UI-free model onto WinUI inlines and blocks. It builds itself as a vertical
    /// stack, so no control template is needed.
    /// </summary>
    public sealed partial class MarkdownPresenter : StackPanel
    {
        private static readonly FontFamily _codeFont = new("Cascadia Mono, Consolas, Courier New");

        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownPresenter),
                new PropertyMetadata(null, OnMarkdownChanged));

        public MarkdownPresenter()
        {
            Spacing = 8;
        }

        public string? Markdown
        {
            get => (string?)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MarkdownPresenter)d).Rebuild();
        }

        private void Rebuild()
        {
            Children.Clear();
            foreach (var block in MarkdownParser.Parse(Markdown))
            {
                var element = BuildBlock(block);
                if (element is not null)
                {
                    Children.Add(element);
                }
            }
        }

        private static FrameworkElement? BuildBlock(MarkdownBlock block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    {
                        var text = NewTextBlock(heading.Level <= 2 ? "SubtitleTextBlockStyle" : "BodyStrongTextBlockStyle");
                        AddInlines(text.Inlines, heading.Inlines);
                        return text;
                    }
                case ParagraphBlock paragraph:
                    {
                        var text = NewTextBlock(styleKey: null);
                        AddInlines(text.Inlines, paragraph.Inlines);
                        return text;
                    }
                case CodeBlock code:
                    return BuildCodeBlock(code.Text);
                case ListBlock list:
                    return BuildList(list);
                default:
                    return null;
            }
        }

        private static TextBlock NewTextBlock(string? styleKey)
        {
            var text = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
            };
            if (styleKey is not null
                && Application.Current.Resources.TryGetValue(styleKey, out var style)
                && style is Style typed)
            {
                text.Style = typed;
            }
            return text;
        }

        private static Border BuildCodeBlock(string code)
        {
            var text = new TextBlock
            {
                Text = code,
                FontFamily = _codeFont,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                IsTextSelectionEnabled = true,
            };
            var border = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Child = text,
            };
            if (Application.Current.Resources.TryGetValue("SubtleFillColorSecondaryBrush", out var brush)
                && brush is Brush typed)
            {
                border.Background = typed;
            }
            return border;
        }

        private static StackPanel BuildList(ListBlock list)
        {
            var container = new StackPanel { Spacing = 4 };
            for (var index = 0; index < list.Items.Count; index++)
            {
                var marker = list.Ordered ? $"{index + 1}." : "•";
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var markerText = new TextBlock { Text = marker, Margin = new Thickness(0, 0, 8, 0) };
                Grid.SetColumn(markerText, 0);

                var itemText = new TextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
                AddInlines(itemText.Inlines, list.Items[index].Inlines);
                Grid.SetColumn(itemText, 1);

                row.Children.Add(markerText);
                row.Children.Add(itemText);
                container.Children.Add(row);
            }
            return container;
        }

        private static void AddInlines(InlineCollection target, IReadOnlyList<MarkdownInline> inlines)
        {
            foreach (var inline in inlines)
            {
                switch (inline)
                {
                    case TextRun run:
                        target.Add(BuildRun(run.Text, run.Bold, run.Italic, run.Code));
                        break;
                    case LinkRun link:
                        target.Add(BuildLink(link));
                        break;
                    case LineBreakRun:
                        target.Add(new LineBreak());
                        break;
                    default:
                        break;
                }
            }
        }

        private static Run BuildRun(string text, bool bold, bool italic, bool code)
        {
            var run = new Run { Text = text };
            if (bold)
            {
                run.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            }
            if (italic)
            {
                run.FontStyle = Windows.UI.Text.FontStyle.Italic;
            }
            if (code)
            {
                run.FontFamily = _codeFont;
            }
            return run;
        }

        private static Inline BuildLink(LinkRun link)
        {
            var inner = BuildRun(link.Text, link.Bold, link.Italic, code: false);
            // Only wrap in a Hyperlink when the URL is a usable absolute URI;
            // otherwise fall back to plain text so a malformed link can't break
            // rendering.
            if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
            {
                var hyperlink = new Hyperlink { NavigateUri = uri };
                hyperlink.Inlines.Add(inner);
                return hyperlink;
            }
            return inner;
        }
    }
}
