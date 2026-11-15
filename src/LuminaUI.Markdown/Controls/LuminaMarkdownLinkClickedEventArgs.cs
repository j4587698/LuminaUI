using System;

namespace LuminaUI.Markdown.Controls;

/// <summary>
/// Describes a link that was clicked inside a <see cref="LuminaMarkdown"/> document.
/// </summary>
public class LuminaMarkdownLinkClickedEventArgs : EventArgs
{
    public LuminaMarkdownLinkClickedEventArgs(Uri uri) => Uri = uri;

    /// <summary>Gets the target of the link.</summary>
    public Uri Uri { get; }

    /// <summary>
    /// Gets or sets whether the click has been handled. When left <c>false</c>, the control opens
    /// the link through the platform launcher for <c>http</c>, <c>https</c> and <c>mailto</c>.
    /// </summary>
    public bool Handled { get; set; }
}
