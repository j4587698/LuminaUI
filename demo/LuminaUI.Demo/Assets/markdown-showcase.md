# Markdown in LuminaUI

This document is loaded from an embedded `.md` resource and rendered by `LuminaMarkdown`. Parsing
is handled by [Markdig](https://github.com/xoofx/markdig); the visual tree, the typography and every
colour come from the Lumina theme.

## Inline formatting

Text can be **bold**, *italic*, ~~struck through~~, or `inline code`, and links such as
[the Avalonia repository](https://github.com/AvaloniaUI/Avalonia) are clickable. Bare URLs are
detected too: https://avaloniaui.net

## Lists

- Bullet lists
- Nested lists
  - indent by two spaces
  - the marker changes per level
- [x] Task lists render as checked boxes
- [ ] and unchecked ones

1. Ordered lists keep their start number
2. Second item
3. Third item

## Code

```csharp
public class LuminaMarkdown : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<LuminaMarkdown, string?>(nameof(Text));
}
```

## Quotes

> The renderer only decides structure and classes. Every colour, size and spacing value comes from
> the theme, so a document follows the semantic brushes and can be restyled by the application.

## Tables

| Element  | Supported | Notes                          |
| :------- | :-------: | ------------------------------ |
| Headings |    yes    | h1 to h6                       |
| Tables   |    yes    | column alignment is respected  |
| Images   |    yes    | rendered through `LuminaImage` |
| Raw HTML |     no    | dropped on purpose             |

---

That is the whole surface: no footnotes, no math, no embedded HTML.
