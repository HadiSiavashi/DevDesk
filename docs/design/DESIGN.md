---
name: DevDesk Aesthetic
colors:
  surface: '#09151d'
  surface-dim: '#09151d'
  surface-bright: '#2f3a44'
  surface-container-lowest: '#040f18'
  surface-container-low: '#111d26'
  surface-container: '#15212a'
  surface-container-high: '#202b35'
  surface-container-highest: '#2b3640'
  on-surface: '#d8e4f1'
  on-surface-variant: '#c3c6d6'
  inverse-surface: '#d8e4f1'
  inverse-on-surface: '#26323b'
  outline: '#8d909f'
  outline-variant: '#434653'
  surface-tint: '#b2c5ff'
  primary: '#b2c5ff'
  on-primary: '#002c72'
  primary-container: '#5b8cff'
  on-primary-container: '#002665'
  inverse-primary: '#1857c8'
  secondary: '#c2c6d0'
  on-secondary: '#2c3138'
  secondary-container: '#474c54'
  on-secondary-container: '#b7bcc6'
  tertiary: '#ffb874'
  on-tertiary: '#4b2800'
  tertiary-container: '#d47b00'
  on-tertiary-container: '#412200'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#dae2ff'
  primary-fixed-dim: '#b2c5ff'
  on-primary-fixed: '#001847'
  on-primary-fixed-variant: '#0040a0'
  secondary-fixed: '#dee2ed'
  secondary-fixed-dim: '#c2c6d0'
  on-secondary-fixed: '#171c23'
  on-secondary-fixed-variant: '#42474f'
  tertiary-fixed: '#ffdcbf'
  tertiary-fixed-dim: '#ffb874'
  on-tertiary-fixed: '#2d1600'
  on-tertiary-fixed-variant: '#6a3b00'
  background: '#09151d'
  on-background: '#d8e4f1'
  surface-variant: '#2b3640'
typography:
  page-title:
    fontFamily: Geist
    fontSize: 19px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.01em
  section-title:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '600'
    lineHeight: 20px
  body:
    fontFamily: Geist
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  metadata:
    fontFamily: Geist
    fontSize: 11px
    fontWeight: '400'
    lineHeight: 16px
  mono-timer:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.02em
  mono-code:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 18px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  xs: 4px
  sm: 8px
  md: 12px
  lg: 16px
  xl: 24px
  xxl: 32px
  gutter: 16px
  sidebar_width: 240px
---

## Brand & Style
This design system is built for a high-performance, keyboard-centric productivity environment. The personality is disciplined, calm, and utilitarian, prioritizing focus over decoration.

The style is **Modern Corporate** with a heavy influence from **Minimalism** and native Windows desktop patterns. It avoids the "web-app" feel by utilizing fixed layouts, subtle borders instead of shadows, and high-density information architecture. The aesthetic response should be one of "quiet power"—fast, responsive, and out of the way. 

Key principles:
- **Density over whitespace:** Information is structured to reduce eye travel.
- **Chrome-less UI:** Borders and tonal shifts define boundaries rather than heavy shadows or containers.
- **Visual hierarchy through typography:** Importance is conveyed via weight and color intensity rather than size.

## Colors
The palette is a deeply saturated "midnight" spectrum. The background uses a near-black slate to minimize eye strain during long coding sessions.

- **Background & Surface:** Use `#0B0F14` for the primary canvas and inputs. Use `#11161D` for elevated containers or cards.
- **Accents:** `#5B8CFF` is used sparingly for primary actions, focus rings, and active states to maintain a calm environment.
- **Status:** Standard semantic colors for Success, Warning, and Error are muted slightly to fit the dark theme's luminosity.
- **RTL Considerations:** Color logic remains identical for Persian interfaces, ensuring that semantic status colors maintain their emotional meaning.

## Typography
The system uses **Geist** for its systematic, developer-friendly proportions, mimicking the precision of Segoe UI but with better legibility in dark modes. **JetBrains Mono** is reserved for all data that requires alignment (timers, IDs, code snippets).

- **Hierarchy:** We use a tight scale. Most UI elements fluctuate between 11px and 14px.
- **Persian (RTL) Support:** When rendering Persian text, ensure a fallback to a high-quality Naskh-based font. Increase line-height by 20% for Persian characters to accommodate ascenders and descenders.
- **Monospace:** Use tabular figures for all timers to prevent visual jitter during countdowns.

## Layout & Spacing
This design system uses a **Fixed Grid** philosophy suitable for a desktop application window.

- **Structure:** A fixed sidebar (`240px`) on the left (or right in RTL) with a flexible main content area.
- **Rhythm:** A 4px baseline grid governs all spacing. Use `8px` for internal component padding and `16px` for layout margins.
- **Density:** Components should favor a "Compact" density model. Lists should use `4px` or `0px` gaps with internal padding to maximize visible items.
- **RTL:** All horizontal spacing, margins, and paddings must flip across the vertical axis. Sidebar anchors to the right, and chevron icons are mirrored.

## Elevation & Depth
Depth is communicated through **Tonal Layers** and **Low-contrast Outlines** rather than shadows.

- **Level 0 (Background):** `#0B0F14` - The deepest layer, used for the main app background and input fields.
- **Level 1 (Sidebar):** `#0E131A` - Slightly raised, providing a clear vertical split.
- **Level 2 (Surface):** `#11161D` - Used for cards, panels, and floating menus.
- **Level 3 (Overlay):** `#171D26` - Used for tooltips and modal dialogs.

**Borders:** Use a 1px solid border (`#26303B`) to define all structural boundaries. Shadows are only permitted for floating context menus (12px blur, 0.3 opacity black) to provide essential separation from the content below.

## Shapes
The shape language is "Soft" yet geometric. It maintains a professional, engineered feel.

- **Buttons & Inputs:** Use `4px` (xs) for a precise, sharp look.
- **Cards & Panels:** Use `6px` (sm) for standard containers.
- **Modals:** Use `8px` (md) for primary application overlays.
- **Selection States:** Items in lists (like a file tree or navigation) should use a `4px` radius for the hover/active background plate.

## Components

- **Buttons:**
  - *Primary:* Solid `#5B8CFF` with `#F3F6F9` text. `4px` radius.
  - *Secondary:* Ghost style with `#26303B` border. 
  - *Keyboard Hints:* Small `11px` Geist Mono tags inside buttons or labels showing shortcuts (e.g., `Ctrl+K`).

- **Inputs:**
  - Background: `#0B0F14`, Border: `#26303B`. On focus, the border changes to `#5B8CFF` with a 0px offset, 2px glow.
  
- **Lists & Navigation:**
  - Active state: Background `#1A2A4A`, left-border accent (or right-border in RTL) of 2px width in `#5B8CFF`.
  - Hover state: Background `#171D26`.

- **Chips / Tags:**
  - Subtle `#171D26` background with `#9AA6B2` text. No borders unless the tag is actionable.

- **Cards:**
  - Background `#11161D`, Border `#26303B`, Radius `6px`. No shadows.

- **Monospace Timers:**
  - Large tabular font (`JetBrains Mono`), color `#F3F6F9`. Used for focus sessions or task durations.

- **Scrollbars:**
  - Minimalist thin bars. Track: transparent, Thumb: `#26303B`. Radius: `10px`.