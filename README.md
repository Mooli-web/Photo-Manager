<div align="center">
  <img src="assets/icon.png" width="110" alt="Photo Manager icon">
  <h1>Photo Manager</h1>
  <p>A private, offline, bilingual and non-destructive photo archive for Windows.</p>
  <p><a href="README.fa.md">فارسی</a> · English</p>
</div>

> **Status:** v1.0 beta. Keep an independent backup of important photographs. The application never edits image contents, but Move mode intentionally moves files after confirmation.

## Features

- Recursive folder scanning with a persistent SQLite catalog
- Grid thumbnails and large preview
- Persian (RTL) and English interfaces
- Add files by **reference**, **copy**, or **move**
- Date-based archive layout (`year/year-month-day`)
- SHA-256 duplicate detection, independent of filename
- Multi-photo tagging, tag removal and partial tag search
- Filename, path and notes search
- 0–5 star rating and rating filter
- EXIF date, camera, lens, dimensions and file size
- Missing-file detection and catalog backup
- Light and dark themes
- Offline operation; no account, cloud or telemetry
- Windows x64 installer and portable executable through GitHub Releases

## Install on Windows

Open the repository's **Releases** page and download:

- `PhotoManager-Setup-1.0.0-x64.exe` for normal installation; or
- the `windows-x64.zip` portable build.

Windows SmartScreen may warn about an unsigned community build. Verify the SHA-256 checksum published with the release, then choose *More info → Run anyway* only if it matches. The project currently has no commercial code-signing certificate.

## Run from source

Requires Python 3.11+.

```powershell
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python -m photomanager
```

Linux/macOS developers can use their platform's activation command. Windows is the supported release target.

## Quick start

1. Select **Add folder** to catalog images without moving them.
2. Select **Import** to reference, copy, or move a complete folder.
3. Select one or multiple thumbnails and add comma-separated tags.
4. Search names/paths/notes, filter tags, or choose a minimum rating.
5. Use **Backup catalog** regularly. This backs up metadata, not original photos.

### Import safety

| Mode | Original file | Archive copy |
|---|---|---|
| Reference | Remains in place | None |
| Copy | Remains in place | Created and organized by date |
| Move | Moved from source | Created and organized by date |

Duplicate content is skipped using SHA-256. Existing destination files are never overwritten; a numeric suffix is added instead. Tags and ratings are stored in the local catalog and do not alter image metadata.

## Supported formats

Full preview and metadata depend on Pillow/Qt decoders. JPG, JPEG, PNG, WebP, BMP, GIF and TIFF are supported by default. HEIC and camera RAW extensions are cataloged, but preview/EXIF support can vary by Windows codec and camera format.

## Data location

The catalog and thumbnail cache use Qt's per-user application data folder, normally:

```text
%LOCALAPPDATA%\Mooli-web\Photo Manager\
```

Uninstalling the application does not delete original photographs. Export a catalog backup before resetting app data.

## Build an EXE locally

```powershell
pip install -r requirements-dev.txt
pytest
pyinstaller --noconfirm PhotoManager.spec
iscc installer.iss
```

The executable appears in `dist`; the installer appears in `installer-output`. Pushing a tag such as `v1.0.0` runs the release workflow and attaches both downloads to a GitHub Release.

## Repository structure

```text
photomanager/       application, catalog, scanner, importer and UI
assets/             icon files
 tests/             database, scanner and import tests
.github/workflows/  CI tests and Windows release build
PhotoManager.spec   PyInstaller configuration
installer.iss       Inno Setup installer
```

## Privacy and limitations

The program makes no network requests. Face recognition, AI tagging, map view, cloud sync and image editing are intentionally outside v1.0. Albums and advanced AND/OR filters are planned. See [ROADMAP.md](ROADMAP.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Please open an issue before a large change. Never include personal photos or catalogs in bug reports.

## License

MIT © 2026 Mooli-web. See [LICENSE](LICENSE).
