# LuminaUI Design System

LuminaUI is built on a modern, high-contrast, and deeply semantic color palette inspired by the best practices of Tailwind CSS (Zinc palette) and Apple/Vercel design guidelines.

## Core Philosophy

1.  **Stop Relying on Muddiness (Muddy Grays)**: In Dark Mode, do not use `#2x2x2x` gray backgrounds to indicate elevation. The background must be pure black (`#000000`) or deep slate (`#09090B`).
2.  **Lines over Blocks**: Use sharp 1px borders (`#27272A`) to define card boundaries and popups instead of large gray backgrounds.
3.  **High Contrast Text**: Text should pop. Use `#FAFAFA` for dark mode text, not `#CCCCCC`.
4.  **Pseudo Glass**: Hardware-accelerated blur is prone to cross-platform failure (e.g., missing GPU contexts). LuminaUI uses 0% CPU "Pseudo Glass" (opacity blending + liquid gradient borders) as its primary aesthetic.

## 1. Neutral Palette (Light Mode)

*   `LuminaBackground` (`#FAFAFA`): The absolute bottom layer of the application window.
*   `LuminaSurface` (`#FFFFFF`): The background of standard cards and content areas.
*   `LuminaSurfaceElevated` (`#FFFFFF`): The background of floating elements (popovers, dropdowns). Relies heavily on Drop Shadow.
*   `LuminaBorder` (`#E4E4E7`): Subtle 1px dividers and card outlines.
*   `LuminaTextForeground` (`#18181B`): Primary text (headings, body).
*   `LuminaTextMuted` (`#71717A`): Secondary text (captions, placeholders).

## 2. Neutral Palette (Dark Mode)

*   `LuminaBackground` (`#000000`): The absolute bottom layer. Deep space black. Excellent for OLED screens and extreme contrast.
*   `LuminaSurface` (`#09090B`): The standard card background. Just 1 step above pure black.
*   `LuminaSurfaceElevated` (`#18181B`): Floating elements. Slightly lighter, but still very dark.
*   `LuminaBorder` (`#27272A`): Critical 1px border that defines shapes against the black background.
*   `LuminaTextForeground` (`#FAFAFA`): Crisply legible white text.
*   `LuminaTextMuted` (`#A1A1AA`): Soft, readable gray text.

## 3. Semantic Colors (Light / Dark)

*   **Primary (Brand)**: `#2563EB` (Blue 600) / `#3B82F6` (Blue 500). Used for primary actions, active states.
*   **Success**: `#16A34A` (Green 600) / `#22C55E` (Green 500). Success messages, confirmations.
*   **Warning**: `#F59E0B` (Amber 500) / `#FBBF24` (Amber 400). Alerts, pending states.
*   **Danger**: `#EF4444` (Red 500) / `#F87171` (Red 400). Destructive actions, errors.

## 4. Elevation & Surfaces

LuminaUI defines 3 levels of elevation for the `LuminaCard` component:

1.  **Solid (Default)**: Flat against the background. Uses `LuminaSurface` and a 1px `LuminaBorder`. Shadow is extremely faint (`0 1 3`).
2.  **Elevated (IsElevated="True")**: Floating above the surface. Uses `LuminaSurfaceElevated`. Casts a distinct drop shadow (`0 4 12`). Ideal for forms and important distinct regions.
3.  **Glass (Classes="Glass")**: The signature Lumina look. Uses a translucent background (`0.40` to `0.85` opacity), a strong drop shadow, and a `LiquidBorderBrush` (a linear gradient that mimics light reflecting off the edge of a glass pane). Zero CPU cost.

## 5. Typography

*   **Headings**: `FontWeight="Black"` or `Bold`. Usually sizes `24`, `28`, `36`.
*   **Subheadings**: `FontWeight="SemiBold"`. Sizes `18`, `20`.
*   **Body**: `FontWeight="Regular"`. Size `14`.
*   **Small**: Size `12`.
