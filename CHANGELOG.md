# Changelog

## Unreleased

- Fixed clipped option labels and controls by replacing the auto-sized options flow with a width-aware grid and scroll area.
- Gave the footer buttons a fixed-height row and sized the startup window to the display's working area and DPI.
- Rebuilt the main window around a pre-download video preview with thumbnail and media details.
- Added persistent filename, metadata, thumbnail and subtitle preferences.
- Switched process invocation to structured arguments so URLs and paths with spaces or query strings are preserved.
- Added a self-contained Windows x64 publishing workflow and documented that the refreshed README screenshot is pending visual verification.
- Added links to project help and the other Voryda utilities.
- Reworked the options column to scroll cleanly on shorter windows and increased the default window size.
- Added in-app source-page playback when supported and a two-handle clip timeline synchronized with timestamp fields.

## 1.0.0 - 2026-09-22

- Initial release
- Video download with quality selection (Best, 1080p, 720p, 480p, Audio only)
- Timestamp-based clipping (start/end)
- Real-time progress display with speed and ETA
- Cancel support mid-download
- Remembered save folder and last settings
- System tray icon with hotkey support
