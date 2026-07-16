# LuminaUI Design System

LuminaUI is built on a modern, high-contrast, and deeply semantic color palette inspired by the best practices of Tailwind CSS neutral palettes and Apple/Vercel design guidelines.

## Core Philosophy

1.  **Stop Relying on Muddiness (Muddy Grays)**: In Dark Mode, do not use `#2x2x2x` gray backgrounds to indicate elevation. The background must be pure black (`#000000`) or deep slate (`#09090B`).
2.  **Lines over Blocks**: Use sharp 1px borders (`#27272A`) to define card boundaries and popups instead of large gray backgrounds.
3.  **High Contrast Text**: Text should pop. Use `#FAFAFA` for dark mode text, not `#CCCCCC`.
4.  **Selectable Glass**: Use `GlassMode` to choose no glass, zero-cost pseudo glass, a one-time cached acrylic snapshot, or dynamic acrylic according to the surface's performance requirements.

## 1. Neutral Palette (Light Mode)

*   `LuminaBackground` (`#F9FAFB`): The absolute bottom layer of the application window.
*   `LuminaSurface` (`#FFFFFF`): The background of standard cards and content areas.
*   `LuminaSurfaceElevated` (`#FFFFFF`): The background of raised cards and elevated panels.
*   `LuminaBorder` (`#E5E7EB`): Subtle 1px dividers and card outlines.
*   `LuminaTextForeground` (`#09090B`): Primary text (headings, body).
*   `LuminaTextMuted` (`#71717A`): Secondary text (captions, placeholders).

## 2. Neutral Palette (Dark Mode)

*   `LuminaBackground` (`#000000`): The absolute bottom layer. Deep space black. Excellent for OLED screens and extreme contrast.
*   `LuminaSurface` (`#09090B`): The standard card background. Just 1 step above pure black.
*   `LuminaSurfaceElevated` (`#18181B`): Floating elements. Slightly lighter, but still very dark.
*   `LuminaBorder` (`#27272A`): Critical 1px border that defines shapes against the black background.
*   `LuminaTextForeground` (`#FAFAFA`): Crisply legible white text.
*   `LuminaTextMuted` (`#A1A1AA`): Soft, readable gray text.

## 3. Semantic Colors (Light / Dark)

*   **Primary (Brand)**: `#2563EB` (Blue 600) / `#3B82F6` (Blue 500). Used for primary actions, active states and runtime accent theming.
*   **Success**: `#16A34A` (Green 600) / `#22C55E` (Green 500). Success messages, confirmations.
*   **Warning**: `#F59E0B` (Amber 500) / `#FBBF24` (Amber 400). Alerts, pending states.
*   **Danger**: `#EF4444` (Red 500) / `#F87171` (Red 400). Destructive actions, errors.

## 4. Elevation & Surfaces

LuminaUI defines 3 levels of elevation for the `LuminaCard` component:

1.  **Solid (Default)**: Flat against the background. Uses `LuminaSurface` and a 1px `LuminaBorder`. Shadow is extremely faint (`0 1 3`).
2.  **Elevated (IsElevated="True")**: Floating above the surface. Uses `LuminaSurfaceElevated`. Casts a distinct drop shadow (`0 4 12`). Ideal for forms and important distinct regions.
3.  **Glass (`GlassMode`)**: `Pseudo` uses a translucent tint, static noise texture, gradient highlight, and gradient edge without backdrop sampling; `AcrylicCached` samples and blurs once, automatically refreshes after a theme-variant change, and can be manually refreshed with `RefreshBackdrop()`; `AcrylicDynamic` performs live backdrop blur. A plain translucent surface uses `Off` with an alpha `Background`. `Classes="Glass"`, `Classes="CachedGlass"`, and `Classes="PseudoGlass"` remain concise aliases for cards.

## 5. Typography

*   **Headings**: `FontWeight="Black"` or `Bold`. Usually sizes `24`, `28`, `36`.
*   **Subheadings**: `FontWeight="SemiBold"`. Sizes `18`, `20`.
*   **Body**: `FontWeight="Regular"`. Size `14`.
*   **Small**: Size `12`.
