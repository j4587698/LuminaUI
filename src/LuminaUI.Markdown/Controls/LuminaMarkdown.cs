using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform.Storage;
using LuminaUI.Markdown.Rendering;
using Markdig;
using Markdig.Syntax;

namespace LuminaUI.Markdown.Controls;

/// <summary>
/// Renders Markdown text with the Lumina visual system.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is delegated to Markdig; the document tree is built here so headings, code blocks,
/// quotes and tables follow the same semantic brushes as the rest of Lumina. Styling lives in
/// <see cref="LuminaMarkdownTheme"/>, so an application can retune the document with styles
/// instead of subclassing the control.
/// </para>
/// <para>
/// The control does not scroll. Wrap it in a <see cref="ScrollViewer"/> when the document is
/// taller than its host, which keeps it usable inside pages that already scroll.
/// </para>
/// <para>
/// Rendered text is selectable and copyable, which is what documentation and chat transcripts
/// need; the trade is one <see cref="SelectableTextBlock"/> per block instead of a plain
/// <see cref="TextBlock"/>.
/// </para>
/// <para>
/// The whole document is rebuilt when <see cref="Text"/> changes. That is the right trade for
/// static documents and for text that is replaced as a whole; for token-by-token streaming, batch
/// the updates instead of assigning on every token.
/// </para>
/// </remarks>
[TemplatePart("PART_DocumentHost", typeof(Panel))]
public class LuminaMarkdown : TemplatedControl
{
    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<LuminaMarkdown, string?>(nameof(Text));

    /// <summary>
    /// The extensions are enabled explicitly rather than through <c>UseAdvancedExtensions</c>, which
    /// also turns on features this renderer has no mapping for (footnotes, math, diagrams).
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .UsePipeTables()
        .UseTaskLists()
        .Build();

    private Panel? _documentHost;

    private bool _isDirty = true;

    /// <summary>Raised when a link in the document is clicked.</summary>
    public event EventHandler<LuminaMarkdownLinkClickedEventArgs>? LinkClicked;

    /// <summary>Gets or sets the Markdown source.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _documentHost = e.NameScope.Find<Panel>("PART_DocumentHost");
        _isDirty = true;
        RenderDocument();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            _isDirty = true;
            RenderDocument();
        }
    }

    private void RenderDocument()
    {
        if (!_isDirty || _documentHost is null)
        {
            return;
        }

        _isDirty = false;
        _documentHost.Children.Clear();

        string? text = Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        MarkdownDocument document = global::Markdig.Markdown.Parse(text, Pipeline);
        MarkdownDocumentRenderer renderer = new();
        renderer.Render(_documentHost.Children, document);

        foreach ((TextBlock block, List<MarkdownLinkSpan> spans) in renderer.LinkBlocks)
        {
            block.AddHandler(
                InputElement.PointerPressedEvent,
                (_, args) => OnLinkPressed(block, spans, args),
                RoutingStrategies.Tunnel);
        }
    }

    /// <remarks>
    /// Avalonia 12 has no <c>Hyperlink</c> inline, so a click is resolved by hit-testing the text
    /// layout and matching the character position against the spans the renderer recorded.
    /// </remarks>
    private void OnLinkPressed(TextBlock block, List<MarkdownLinkSpan> spans, PointerPressedEventArgs args)
    {
        if (block.TextLayout is not { } layout || !args.GetCurrentPoint(block).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Point position = args.GetPosition(block);
        TextHitTestResult hit = layout.HitTestPoint(position);

        if (!hit.IsInside)
        {
            return;
        }

        foreach (MarkdownLinkSpan span in spans)
        {
            if (hit.TextPosition < span.Start || hit.TextPosition >= span.Start + span.Length)
            {
                continue;
            }

            if (Uri.TryCreate(span.Url, UriKind.Absolute, out Uri? uri))
            {
                args.Handled = true;
                RequestLink(uri);
            }

            return;
        }
    }

    private void RequestLink(Uri uri)
    {
        LuminaMarkdownLinkClickedEventArgs args = new(uri);
        LinkClicked?.Invoke(this, args);

        if (args.Handled || uri.Scheme is not ("http" or "https" or "mailto"))
        {
            return;
        }

        if (TopLevel.GetTopLevel(this)?.Launcher is not { } launcher)
        {
            return;
        }

        _ = LaunchAsync(launcher, uri);
    }

    private static async Task LaunchAsync(ILauncher launcher, Uri uri)
    {
        try
        {
            await launcher.LaunchUriAsync(uri);
        }
        catch (Exception)
        {
            // No handler for the scheme on this platform, or the host blocked it. Not fatal for a
            // document renderer: the application can still handle LinkClicked itself.
        }
    }
}
