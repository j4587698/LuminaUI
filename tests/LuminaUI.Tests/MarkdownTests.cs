using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Markdown.Controls;
using Xunit;

namespace LuminaUI.Tests;

/// <summary>
/// Covers the Markdown package end to end: the document tree Markdig produces, the styles that
/// make it look like Lumina, and the link hit-testing that replaces Avalonia's missing
/// <c>Hyperlink</c> inline.
/// </summary>
public class MarkdownTests
{
    private const double SurfaceWidth = 520;
    private const double SurfaceHeight = 900;

    private const string Document = """
        # Document title

        A paragraph with **bold**, *italic*, `inline code` and a [link](https://example.com/docs).

        ## Lists

        - first
        - second
          - nested

        1. one
        2. two

        > quoted text

        ```csharp
        var answer = 42;
        ```

        | Left | Center | Right |
        | :--- | :----: | ----: |
        | a    | b      | c     |

        ---
        """;

    [AvaloniaFact]
    public void RendersDocumentWithoutLogging()
    {
        using LogCapture log = LogCapture.Start();
        Window window = Show(new LuminaMarkdown { Text = Document });

        try
        {
            List<Control> controls = [.. window.GetVisualDescendants().OfType<Control>()];

            Assert.Contains(controls, control => HasClass(control, "md-h1"));
            Assert.Contains(controls, control => HasClass(control, "md-h2"));
            Assert.Contains(controls, control => HasClass(control, "md-quote"));
            Assert.Contains(controls, control => HasClass(control, "md-code-block"));
            Assert.Contains(controls, control => HasClass(control, "md-table-frame"));
            Assert.Contains(controls, control => HasClass(control, "md-rule"));

            // Two unordered items, one nested item, two ordered items.
            Assert.Equal(5, controls.Count(control => HasClass(control, "md-list-marker")));

            Assert.Contains(
                controls,
                control => control is TextBlock { Text: "var answer = 42;" } text && HasClass(text, "md-code-text"));

            // A table cell is a Border inside the frame's Grid.
            Assert.Equal(
                6,
                controls.OfType<Border>().Count(border => HasClass(border, "md-table-cell")));
        }
        finally
        {
            window.Close();
        }

        Assert.True(log.Entries.Count == 0, string.Join(Environment.NewLine, log.Entries));
    }

    /// <remarks>
    /// Inline styling is the one part of the theme that can fail silently: if a class-based style
    /// does not match a <see cref="Run"/>, the link renders in body colour with no underline and
    /// nothing is logged. Asserting the resolved values is the only way to catch that.
    /// </remarks>
    [AvaloniaFact]
    public void InlineRunsPickUpThemedStyles()
    {
        Window window = Show(new LuminaMarkdown { Text = "Text with a [link](https://example.com) and `code`." });

        try
        {
            List<Run> runs = [.. RunsIn(window)];

            Run link = Assert.Single(runs, run => run.Classes.Contains("md-link"));
            Run code = Assert.Single(runs, run => run.Classes.Contains("md-code"));

            Assert.NotNull(link.Foreground);
            Assert.NotNull(link.TextDecorations);
            Assert.NotNull(code.Background);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ClickingLinkTextRaisesLinkClicked()
    {
        LuminaMarkdown markdown = new() { Text = "See [the docs](https://example.com/docs) now." };
        Uri? clicked = null;
        markdown.LinkClicked += (_, args) =>
        {
            clicked = args.Uri;
            args.Handled = true;
        };

        Window window = Show(markdown);

        try
        {
            TextBlock paragraph = window
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Single(text => text.Classes.Contains("md-paragraph"));

            int offset = 0;
            int length = 0;

            foreach (Run run in paragraph.Inlines!.OfType<Run>())
            {
                if (run.Classes.Contains("md-link"))
                {
                    length = run.Text?.Length ?? 0;
                    break;
                }

                offset += run.Text?.Length ?? 0;
            }

            Assert.True(length > 0, "The paragraph should contain a link run.");

            Rect bounds = paragraph.TextLayout!.HitTestTextRange(offset, length).First();
            Point point = paragraph.TranslatePoint(bounds.Center, window) ?? bounds.Center;

            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            window.Close();
        }

        Assert.Equal(new Uri("https://example.com/docs"), clicked);
    }

    /// <remarks>
    /// A type selector only matches the exact control type, so a theme written for
    /// <see cref="TextBlock"/> silently skips the <see cref="SelectableTextBlock"/> the renderer
    /// actually creates: headings collapse to body size and paragraphs stop wrapping, with nothing
    /// logged. These assertions are what keeps that from shipping again.
    /// </remarks>
    [AvaloniaFact]
    public void RenderedTextPicksUpThemedTypography()
    {
        Window window = Show(new LuminaMarkdown { Text = "# Heading\n\nBody text." });

        try
        {
            List<SelectableTextBlock> blocks = [.. window.GetVisualDescendants().OfType<SelectableTextBlock>()];

            SelectableTextBlock heading = Assert.Single(blocks, text => text.Classes.Contains("md-h1"));
            SelectableTextBlock paragraph = Assert.Single(blocks, text => text.Classes.Contains("md-paragraph"));

            Assert.True(
                heading.FontSize > paragraph.FontSize,
                $"The heading should be larger than the body: {heading.FontSize} vs {paragraph.FontSize}.");
            Assert.Equal(FontWeight.Bold, heading.FontWeight);
            Assert.Equal(TextWrapping.Wrap, paragraph.TextWrapping);
            Assert.NotNull(paragraph.Foreground);
        }
        finally
        {
            window.Close();
        }
    }

    /// <remarks>
    /// The first version wrote U+2610 and U+2611 as text. Those two code points are served by
    /// different fallback fonts, so the checked and unchecked boxes came out with different sizes
    /// and stroke weights. The marker is now a real <see cref="CheckBox"/> on the core package's
    /// compact theme, which also has to be small enough not to inflate the line.
    /// </remarks>
    [AvaloniaFact]
    public void TaskListMarkersUseOneThemedBox()
    {
        Window window = Show(new LuminaMarkdown { Text = "- [x] done\n- [ ] todo" });

        try
        {
            List<CheckBox> boxes = [.. window.GetVisualDescendants().OfType<CheckBox>()];

            Assert.Equal(2, boxes.Count);
            Assert.All(boxes, box => Assert.Equal(boxes[0].Bounds.Size, box.Bounds.Size));

            CheckBox checkedBox = Assert.Single(boxes, box => box.IsChecked == true);
            CheckBox uncheckedBox = Assert.Single(boxes, box => box.IsChecked == false);

            // The compact theme, not the default 32px touch target.
            Assert.All(boxes, box => Assert.Equal(new Size(20, 20), box.Bounds.Size));
            Assert.NotNull(boxes[0].Theme);

            // The checkmark lives in the template and is only visible when checked.
            Avalonia.Controls.Shapes.Path checkMark =
                Assert.Single(checkedBox.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>());
            Assert.True(checkMark.IsVisible);
            Assert.DoesNotContain(uncheckedBox.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>(), p => p.IsVisible);

            // The glyphs must be gone, not merely hidden behind the box.
            Assert.DoesNotContain(
                window.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text is { } value && (value.Contains('\u2611') || value.Contains('\u2610')));
        }
        finally
        {
            window.Close();
        }
    }

    /// <remarks>
    /// A marker without a line height is laid out at the font's natural height while the paragraph
    /// uses the theme's 23px line, so the number floats above the text. Both must start at the same
    /// top offset for the first line to share a baseline.
    /// </remarks>
    [AvaloniaFact]
    public void ListMarkersShareTheFirstLineOfTheirItem()
    {
        Window window = Show(new LuminaMarkdown { Text = "1. one\n2. two\n\n- [x] task" });

        try
        {
            List<Control> markers = [.. window.GetVisualDescendants().OfType<Control>().Where(c => HasClass(c, "md-list-marker"))];
            Assert.Equal(2, markers.Count);

            foreach (Control marker in markers)
            {
                Grid row = Assert.IsType<Grid>(marker.Parent);
                TextBlock paragraph = row.GetVisualDescendants().OfType<TextBlock>().Single(t => HasClass(t, "md-paragraph"));

                // The paragraph sits in a StackPanel, so compare in the row's coordinates.
                double markerTop = marker.TranslatePoint(default, row)!.Value.Y;
                double textTop = paragraph.TranslatePoint(default, row)!.Value.Y;

                Assert.Equal(textTop, markerTop, precision: 1);
            }

            CheckBox task = Assert.Single(window.GetVisualDescendants().OfType<CheckBox>());
            Grid taskRow = Assert.IsType<Grid>(task.Parent);
            TextBlock taskText = taskRow.GetVisualDescendants().OfType<TextBlock>().Single(t => HasClass(t, "md-paragraph"));

            // The box is shorter than the line, so it is centred rather than top-aligned.
            double taskTop = task.TranslatePoint(default, taskRow)!.Value.Y;
            double taskTextTop = taskText.TranslatePoint(default, taskRow)!.Value.Y;

            Assert.InRange(taskTop, taskTextTop - 1, taskTextTop + 6);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EmptyTextRendersNothing()
    {
        using LogCapture log = LogCapture.Start();
        Window window = Show(new LuminaMarkdown());

        try
        {
            Assert.DoesNotContain(
                window.GetVisualDescendants().OfType<TextBlock>(),
                text => text.Classes.Contains("md-paragraph"));
        }
        finally
        {
            window.Close();
        }

        Assert.True(log.Entries.Count == 0, string.Join(Environment.NewLine, log.Entries));
    }

    private static IEnumerable<Run> RunsIn(Window window) =>
        window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .SelectMany(text => text.Inlines is null ? [] : text.Inlines.OfType<Run>());

    private static bool HasClass(Control control, string name) => control.Classes.Contains(name);

    private static Window Show(Control content)
    {
        Window window = new() { Width = SurfaceWidth, Height = SurfaceHeight, Content = content };
        window.Show();
        window.Measure(new Size(SurfaceWidth, SurfaceHeight));
        window.Arrange(new Rect(0, 0, SurfaceWidth, SurfaceHeight));
        Dispatcher.UIThread.RunJobs();

        return window;
    }
}
