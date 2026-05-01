# Changelog

---

## [v2.0.0] - 2026-05-01 "The UX & Precision Update"

### Added

- **Drag & Drop Support:** Drop GIF or animated WebP files directly into the management window or the main form to add new overlays instantly.
- **Global Z-Order Management:** New "Always on Top" toggle to alternate between floating overlays (TopMost) or background desktop decorations (Wallpaper mode).
- **JSON Presets System:** Full support for exporting and importing your entire overlay configuration. Share your setups or switch between different themes easily via `.json` files.
- **Full Internationalization (i18n):** Complete translation of the user interface to English, including context menus, tooltips, and dynamic labels.
- **Centralized "Add Overlay" Logic:** Improved placement algorithm that prevents overlays from overlapping when added and respects screen boundaries.

### Improved

- **Modern UI Refactor:** Migrated to a responsive `FlowLayoutPanel` system with `AutoSize` support, ensuring the UI looks sharp on different DPI settings and window sizes.
- **Dark Mode Visibility:** Fixed a native WinForms issue where disabled checkboxes/buttons were unreadable in dark themes. Implemented a custom "Safe-Disabled" state with color-coded feedback.
- **Stand-alone Distribution:** Removed external `.ico` dependencies. The application now generates its own high-quality dynamic icons for the System Tray, facilitating a single `.exe` distribution.
- **Code Robustness:** Refactored internal control references to eliminate lookup errors and improve synchronization between the Chroma Key toggle and the Tolerance slider.
- **Build Pipeline:** Optimized for .NET 9.0 with `SingleFile` publishing support.

---

## [v1.2.0] - 2026-01-28 "The Visual Update"

### Added

- **Animated WebP Support:** Full integration with `ImageSharp` to play `.webp` files with transparency and variable frame times.
- **Static Image Support:** Support for loading `.png`, `.jpg`, and `.jpeg` files as fixed overlays.
- **Advanced Chroma Key:**
  - Independent "Use Chroma Key" option per overlay.
  - **Eyedropper Tool:** Select the exact background color by clicking directly on the overlay.
  - **Color Picker:** Manual color selection via standard dialog.
  - **Tolerance Slider:** Precision adjustment (10-150) to smooth edges and remove similar colors.
- **Interactive Eyedropper Mode:** Crosshair cursor when selecting colors for better precision.

### Improved

- The tolerance slider now only enables when Chroma Key is active.
- Data persistence updated to include Chroma and tolerance settings.

---

## [v1.1.0] - 2026-01-27 "Performance & Grid"

### Added

- **Grid UI:** New grid interface with thumbnails to manage overlays.
- **Smart Pause:** System tray option to pause/resume all animations and free up CPU/GPU resources.
- **Transparent Preview:** Thumbnails in the grid respect the transparency of the original file.

### Fixed

- **Smooth Initialization:** Eliminated black/white flickering when opening new overlays; they now appear at the correct size and position instantly.
- **Icons:** Integrated icons as embedded resources for single-file builds.

---

## [v1.0.0] - 2026-01-26 "Initial Release"

### Added

- **Real Transparency Rendering:** Implementation based on Layered Windows and the Win32 API to ensure perfect alpha channel composition over any window or the desktop.
- **Configuration Management:** Automatic storage of overlay states in JSON format, allowing persistence between sessions.
- **Global Dimension Control:** Centralized adjustment of animation height to ensure visual uniformity across the entire configuration.
- **Background Interface:** Operation from the notification area (System Tray), minimizing interference with the main workflow.
- **Workspace Adaptation:** Optional capability to position elements while respecting the dimensions of the Windows taskbar.

## System Requirements

- **Operating System:** Windows 10 or Windows 11 (64-bit Architecture).
- **Runtime Environment:** .NET 9.0 (included in the self-contained version).

## Installation Instructions

- **Self-Contained Version:** Requires no additional installations. Includes all necessary dependencies within a single executable.
