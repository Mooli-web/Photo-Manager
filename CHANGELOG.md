# Changelog

## [2.0.0-beta.1] - Unreleased

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
