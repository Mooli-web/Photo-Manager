# Changelog

## [2.0.0-beta.3] - Unreleased

### Changed
- Improved the WPF dark theme contrast so list items, paths, counters and side-panel controls stay readable.
- Reworked the toolbar and filter row spacing to prevent button text from being clipped or washed out.
- Made virtualized list items stretch to the full available width and use readable card backgrounds for normal, hover and selected states.
- Preserved left-to-right rendering for filenames, file sizes and Windows paths inside the Persian interface.

## [2.0.0-beta.2]

### Fixed
- Marked the read-only `ProgressValue` WPF binding as explicitly OneWay, preventing an `InvalidOperationException` during window startup.
- Added a Windows WPF startup/binding smoke test to both CI and the release gate.

## [2.0.0-beta.1]

### Changed
- Replaced the Python/PyQt prototype with C#/.NET 10/WPF and MVVM boundaries.
- Moved folder enumeration, metadata, hashing, thumbnails and SQLite writes off the UI thread.
- Added bounded channels, cancellation, progress, batching, paging and WPF recycling virtualization.
- Changed duplicate detection to quick fingerprints with full SHA-256 confirmation only when needed.
- Release outputs are self-contained single-file builds for Windows x64 and ARM64.

### Safety
- Copy/Move import is temporarily removed until transactional journaling and verification are complete.
- v2 uses a new catalog and does not mutate v1 data.

## [1.0.0] - 2026-08-23
- Initial Python/PyQt prototype release. Deprecated for large folders.
