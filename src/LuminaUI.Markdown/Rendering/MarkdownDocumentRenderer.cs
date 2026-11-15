using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Controls;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;

namespace LuminaUI.Markdown.Rendering;

/// <summary>
/// A run of characters inside a rendered text block that opens a URL when clicked.
/// </summary>
/// <param name="Start">Character offset of the first character of the link.</param>
/// <param name="Length">Length of the link text.</param>
/// <param name="Url">Target of the link.</param>
internal readonly record struct MarkdownLinkSpan(int Start, int Length, string Url);

/// <summary>
/// Maps a Markdig document onto Lumina controls.
/// </summary>
/// <remarks>
/// <para>
/// The renderer only decides structure and <c>md-*</c> classes; every colour, size and spacing
/// value comes from <c>LuminaMarkdownTheme</c>, so a document follows the semantic brushes and can
/// be restyled by the application.
/// </para>
/// <para>
/// Links are plain <see cref="Run"/>s: Avalonia 12 has no <c>Hyperlink</c> inline, so the owner
/// resolves a click by hit-testing the text layout against the spans collected here.
/// </para>
/// </remarks>
internal sealed class MarkdownDocumentRenderer
{
    private const string TextClass = "md-text";

    private readonly List<(TextBlock Block, List<MarkdownLinkSpan> Spans)> _linkBlocks = [];

    /// <summary>
    /// Text blocks that contain at least one link, with the spans to resolve clicks against.
    /// </summary>
    public IReadOnlyList<(TextBlock Block, List<MarkdownLinkSpan> Spans)> LinkBlocks => _linkBlocks;

    public void Render(IList<Control> target, MarkdownDocument document)
    {
        foreach (Control control in RenderBlocks(document, 0))
        {
            target.Add(control);
        }
    }

    private IEnumerable<Control> RenderBlocks(IEnumerable<Block> blocks, int depth)
    {
        foreach (Block block in blocks)
        {
            Control? control = RenderBlock(block, depth);

            if (control is not null)
            {
                yield return control;
            }
        }
    }

    private Control? RenderBlock(Block block, int depth) =>
        block switch
        {
            HeadingBlock heading => RenderHeading(heading),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            ListBlock list => RenderList(list, depth),
            QuoteBlock quote => RenderQuote(quote, depth),
            FencedCodeBlock fenced => RenderCodeBlock(fenced, fenced.Info),
            CodeBlock code => RenderCodeBlock(code, null),
            ThematicBreakBlock => RenderRule(),
            // Raw HTML and link reference definitions have no visual representation.
            HtmlBlock => null,
            LinkReferenceDefinitionGroup => null,
            Table table => RenderTable(table),
            ContainerBlock container => RenderContainer(container, depth),
            _ => null,
        };

    private Control RenderContainer(ContainerBlock container, int depth)
    {
        StackPanel panel = new() { Orientation = Orientation.Vertical };

        foreach (Control control in RenderBlocks(container, depth))
        {
            panel.Children.Add(control);
        }

        return panel;
    }

    private Control RenderHeading(HeadingBlock heading)
    {
        int level = Math.Clamp(heading.Level, 1, 6);
        TextBlock text = CreateTextBlock("md-h" + level.ToString(CultureInfo.InvariantCulture));
        BuildInlines(text, heading.Inline);

        return text;
    }

    private Control RenderParagraph(ParagraphBlock paragraph)
    {
        // A paragraph whose only content is an image is a block-level image, not a text line.
        if (paragraph.Inline is { FirstChild: LinkInline { IsImage: true } image, LastChild: LinkInline last }
            && ReferenceEquals(image, last))
        {
            return RenderImage(image.Url, GetAltText(image));
        }

        TextBlock text = CreateTextBlock("md-paragraph");
        BuildInlines(text, paragraph.Inline);

        return text;
    }

    private Control RenderList(ListBlock list, int depth)
    {
        StackPanel container = new() { Orientation = Orientation.Vertical };

        int start = 1;

        if (list.IsOrdered
            && !string.IsNullOrEmpty(list.OrderedStart)
            && int.TryParse(list.OrderedStart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            start = parsed;
        }

        int index = 0;

        foreach (Block child in list)
        {
            if (child is not ListItemBlock item)
            {
                continue;
            }

            Grid row = new()
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                Classes = { "md-list-item" },
            };

            Control marker = CreateListMarker(list, item, depth, start + index);
            marker.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(marker, 0);
            row.Children.Add(marker);

            StackPanel content = new() { Orientation = Orientation.Vertical };

            foreach (Control control in RenderBlocks(item, depth + 1))
            {
                if (control is TextBlock text)
                {
                    // The item's rhythm comes from md-list-item; a paragraph's own bottom margin
                    // would double it and push every entry apart.
                    text.Margin = default;
                }

                content.Children.Add(control);
            }

            Grid.SetColumn(content, 1);
            row.Children.Add(content);

            container.Children.Add(row);
            index++;
        }

        return container;
    }

    /// <remarks>
    /// A task item is a real <see cref="CheckBox"/> using the compact theme from the core package:
    /// the default checkbox template reserves a 32px row for touch input and no setter can shrink
    /// it, which would push every entry of a 23px document line apart. The compact theme keeps the
    /// control, its states and its semantic brushes, and only changes the box size.
    /// </remarks>
    private static Control CreateListMarker(ListBlock list, ListItemBlock item, int depth, int number)
    {
        if (GetTaskState(item) is bool isChecked)
        {
            return new CheckBox { IsChecked = isChecked, Classes = { "md-task" } };
        }

        TextBlock marker = CreateTextBlock("md-list-marker");
        marker.Text = list.IsOrdered
            ? number.ToString(CultureInfo.InvariantCulture) + "."
            : GetBullet(depth);

        // Numbers share a right edge so "1." and "10." line their text up. A bullet starts at the
        // same left edge as a task checkbox, so both kinds of marker share one indentation.
        marker.TextAlignment = list.IsOrdered ? TextAlignment.Right : TextAlignment.Left;

        return marker;
    }

    /// <summary>
    /// Returns the checked state when the item starts with a GFM task list marker, otherwise
    /// <c>null</c>.
    /// </summary>
    private static bool? GetTaskState(ListItemBlock item)
    {
        foreach (Block child in item)
        {
            if (child is not LeafBlock { Inline: { } inline })
            {
                continue;
            }

            // The marker is always the first inline of the item's first paragraph.
            return inline.FirstChild is TaskList task ? task.Checked : null;
        }

        return null;
    }

    private Control RenderQuote(QuoteBlock quote, int depth)
    {
        StackPanel content = new() { Orientation = Orientation.Vertical };

        foreach (Control control in RenderBlocks(quote, depth))
        {
            content.Children.Add(control);
        }

        return new Border { Child = content, Classes = { "md-quote" } };
    }

    private Control RenderCodeBlock(LeafBlock block, string? info)
    {
        StackPanel content = new() { Orientation = Orientation.Vertical };

        string? language = GetLanguage(info);

        if (language is not null)
        {
            TextBlock label = CreateTextBlock("md-code-language");
            label.Text = language;
            content.Children.Add(label);
        }

        TextBlock code = CreateTextBlock("md-code-text");
        code.TextWrapping = TextWrapping.NoWrap;
        code.Text = GetCodeText(block);

        content.Children.Add(new ScrollViewer
        {
            Content = code,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
        });

        return new Border { Child = content, Classes = { "md-code-block" } };
    }

    private static Control RenderRule() => new Border { Classes = { "md-rule" } };

    private Control RenderTable(Table table)
    {
        int columnCount = GetColumnCount(table);
        Grid grid = new();

        for (int column = 0; column < columnCount; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        int rowIndex = 0;
        int lastRowIndex = GetRowCount(table) - 1;

        foreach (Block child in table)
        {
            if (child is not TableRow row)
            {
                continue;
            }

            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            // Markdig leaves TableCell.ColumnIndex at -1 for plain pipe tables, so the column is
            // tracked here: every cell advances by its span.
            int column = 0;

            foreach (Block cellBlock in row)
            {
                if (cellBlock is not TableCell cell)
                {
                    continue;
                }

                int columnSpan = Math.Max(cell.ColumnSpan, 1);
                Border cellBorder = new() { Classes = { "md-table-cell" } };

                if (row.IsHeader)
                {
                    cellBorder.Classes.Add("md-table-header-cell");
                }

                // The last row keeps the frame as its bottom edge instead of drawing a second line.
                if (rowIndex == lastRowIndex)
                {
                    cellBorder.BorderThickness = default;
                }

                StackPanel content = new() { Orientation = Orientation.Vertical };
                TextAlignment alignment = GetCellAlignment(table, column);

                foreach (Control control in RenderBlocks(cell, 0))
                {
                    if (control is TextBlock text)
                    {
                        // Cell text hugs its padding: the block spacing belongs between blocks.
                        text.Margin = default;
                        text.TextAlignment = alignment;

                        if (row.IsHeader)
                        {
                            text.FontWeight = FontWeight.SemiBold;
                        }
                    }

                    content.Children.Add(control);
                }

                cellBorder.Child = content;

                Grid.SetColumn(cellBorder, column);
                Grid.SetRow(cellBorder, rowIndex);

                if (columnSpan > 1)
                {
                    Grid.SetColumnSpan(cellBorder, columnSpan);
                }

                if (cell.RowSpan > 1)
                {
                    Grid.SetRowSpan(cellBorder, cell.RowSpan);
                }

                grid.Children.Add(cellBorder);
                column += columnSpan;
            }

            rowIndex++;
        }

        return new Border { Child = grid, Classes = { "md-table-frame" } };
    }

    private Control RenderImage(string? url, string? altText)
    {
        if (string.IsNullOrWhiteSpace(url) || !IsSupportedImageUrl(url))
        {
            return CreateAltText(altText, url);
        }

        return new LuminaImage
        {
            Source = url,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            Classes = { "md-image" },
        };
    }

    private Control CreateAltText(string? altText, string? url)
    {
        TextBlock text = CreateTextBlock("md-paragraph");
        string label = string.IsNullOrEmpty(altText) ? url ?? string.Empty : altText;
        Append(new InlineWriter(text), label, new InlineStyle(Italic: true, Link: true));

        return text;
    }

    /// <remarks>
    /// Text is always rendered through <see cref="SelectableTextBlock"/>: documents exist to be
    /// copied from, and the theme can only target one text type per style (a type selector does not
    /// match derived controls), so keeping a second, plain path would mean duplicating the theme.
    /// </remarks>
    private static TextBlock CreateTextBlock(string roleClass)
    {
        SelectableTextBlock block = new();
        block.Classes.Add(TextClass);
        block.Classes.Add(roleClass);

        return block;
    }

    private void BuildInlines(TextBlock target, ContainerInline? container)
    {
        InlineWriter writer = new(target);
        WriteInlines(writer, container, default);

        if (writer.Spans is { Count: > 0 })
        {
            _linkBlocks.Add((target, writer.Spans));
        }
    }

    private void WriteInlines(InlineWriter writer, ContainerInline? container, InlineStyle style)
    {
        if (container is null)
        {
            return;
        }

        for (MarkdigInline? inline = container.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    Append(writer, literal.Content.ToString(), style);
                    break;

                case EmphasisInline emphasis:
                    WriteInlines(writer, emphasis, ApplyEmphasis(style, emphasis));
                    break;

                case CodeInline code:
                    Append(writer, code.Content, style with { Code = true });
                    break;

                case LineBreakInline lineBreak:
                    if (lineBreak.IsHard)
                    {
                        writer.AppendBreak();
                    }
                    else
                    {
                        Append(writer, " ", style);
                    }

                    break;

                case LinkInline { IsImage: true } image:
                    // Inline images cannot flow with the text, so the alt text stands in for them.
                    Append(writer, GetAltText(image), style with { Italic = true, Link = true });
                    break;

                case LinkInline link:
                    WriteLink(writer, link, style);
                    break;

                case AutolinkInline autolink:
                    WriteAutolink(writer, autolink, style);
                    break;

                case TaskList:
                    // The box is drawn by the list marker, so the inline itself contributes no
                    // characters.
                    break;

                case ContainerInline nested:
                    WriteInlines(writer, nested, style);
                    break;
            }
        }
    }

    private void WriteLink(InlineWriter writer, LinkInline link, InlineStyle style)
    {
        int start = writer.Offset;
        WriteInlines(writer, link, style with { Link = true });

        int length = writer.Offset - start;

        if (length > 0 && !string.IsNullOrEmpty(link.Url))
        {
            writer.AddSpan(start, length, link.Url);
        }
    }

    private static void WriteAutolink(InlineWriter writer, AutolinkInline autolink, InlineStyle style)
    {
        string text = autolink.Url;
        string url = autolink.IsEmail ? "mailto:" + text : text;

        int start = writer.Offset;
        Append(writer, text, style with { Link = true });

        if (writer.Offset > start)
        {
            writer.AddSpan(start, writer.Offset - start, url);
        }
    }

    private static InlineStyle ApplyEmphasis(InlineStyle style, EmphasisInline emphasis) =>
        emphasis.DelimiterChar switch
        {
            '~' when emphasis.DelimiterCount >= 2 => style with { Strike = true },
            '~' => style,
            _ => emphasis.DelimiterCount switch
            {
                1 => style with { Italic = true },
                2 => style with { Bold = true },
                _ => style with { Bold = true, Italic = true },
            },
        };

    private static void Append(InlineWriter writer, string? text, InlineStyle style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Run run = new() { Text = text };

        if (style.Bold)
        {
            run.FontWeight = FontWeight.Bold;
        }

        if (style.Italic)
        {
            run.FontStyle = FontStyle.Italic;
        }

        if (style.Strike)
        {
            run.TextDecorations = TextDecorations.Strikethrough;
        }

        if (style.Code)
        {
            run.Classes.Add("md-code");
        }

        if (style.Link)
        {
            run.Classes.Add("md-link");
        }

        writer.Append(run, text.Length);
    }

    private static string? GetLanguage(string? info)
    {
        if (string.IsNullOrWhiteSpace(info))
        {
            return null;
        }

        // The info string may carry extra attributes after the language token.
        string trimmed = info.Trim();
        int separator = trimmed.IndexOf(' ');

        return separator > 0 ? trimmed[..separator] : trimmed;
    }

    private static string GetCodeText(LeafBlock block)
    {
        StringLineGroup lines = block.Lines;
        StringBuilder builder = new();

        for (int index = 0; index < lines.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('\n');
            }

            builder.Append(lines.Lines[index].ToString());
        }

        return builder.ToString();
    }

    private static string? GetAltText(LinkInline image)
    {
        StringBuilder builder = new();

        for (MarkdigInline? inline = image.FirstChild; inline is not null; inline = inline.NextSibling)
        {
            if (inline is LiteralInline literal)
            {
                builder.Append(literal.Content.ToString());
            }
            else if (inline is ContainerInline container)
            {
                for (MarkdigInline? nested = container.FirstChild; nested is not null; nested = nested.NextSibling)
                {
                    if (nested is LiteralInline nestedLiteral)
                    {
                        builder.Append(nestedLiteral.Content.ToString());
                    }
                }
            }
        }

        return builder.Length > 0 ? builder.ToString() : null;
    }

    private static bool IsSupportedImageUrl(string url) =>
        !url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
        && !url.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    private static string GetBullet(int depth) => depth switch
    {
        0 => "\u2022",
        1 => "\u25E6",
        _ => "\u25AA",
    };

    private static int GetColumnCount(Table table)
    {
        int count = table.ColumnDefinitions.Count;

        foreach (Block child in table)
        {
            if (child is not TableRow row)
            {
                continue;
            }

            int rowColumns = 0;

            foreach (Block cellBlock in row)
            {
                if (cellBlock is TableCell cell)
                {
                    rowColumns += Math.Max(cell.ColumnSpan, 1);
                }
            }

            count = Math.Max(count, rowColumns);
        }

        return Math.Max(count, 1);
    }

    private static int GetRowCount(Table table)
    {
        int count = 0;

        foreach (Block child in table)
        {
            if (child is TableRow)
            {
                count++;
            }
        }

        return count;
    }

    private static TextAlignment GetCellAlignment(Table table, int column)
    {
        if (column < 0 || column >= table.ColumnDefinitions.Count)
        {
            return TextAlignment.Left;
        }

        return table.ColumnDefinitions[column].Alignment switch
        {
            TableColumnAlign.Center => TextAlignment.Center,
            TableColumnAlign.Right => TextAlignment.Right,
            _ => TextAlignment.Left,
        };
    }

    /// <summary>Inline formatting inherited down the emphasis tree.</summary>
    private readonly record struct InlineStyle(
        bool Bold = false,
        bool Italic = false,
        bool Strike = false,
        bool Code = false,
        bool Link = false);

    /// <summary>Tracks the character offset of the inlines appended to one text block.</summary>
    private sealed class InlineWriter(TextBlock target)
    {
        public TextBlock Target { get; } = target;

        public int Offset { get; private set; }

        public List<MarkdownLinkSpan>? Spans { get; private set; }

        public void Append(Run run, int length)
        {
            Target.Inlines!.Add(run);
            Offset += length;
        }

        public void AppendBreak()
        {
            Target.Inlines!.Add(new LineBreak());
            Offset += 1;
        }

        public void AddSpan(int start, int length, string url) =>
            (Spans ??= []).Add(new MarkdownLinkSpan(start, length, url));
    }
}
