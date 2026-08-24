<div align="center">
  <img src="assets/icon.png" width="110" alt="Photo Manager icon">
  <h1>Photo Manager 2</h1>
  <p>Fast, private, bilingual photo catalog for Windows — rebuilt with C#/.NET 10 and WPF.</p>
  <p><a href="README.fa.md">فارسی</a> · English</p>
</div>

> **v2 is a pre-release migration.** The Python v1 release remains available, but is not recommended for large folders. Never keep your only copy of important photographs in any catalog application.

## Why v2?

The prototype performed disk scanning, full SHA-256 hashing, metadata extraction, database writes and thumbnail generation on the UI thread. Large folders could make Windows report the application as unresponsive. v2 replaces that execution model rather than patching it.

## Architecture

- **C# / .NET 10 LTS / WPF / MVVM**
- Domain, Application, Infrastructure and Presentation projects
- Bounded asynchronous channels for scanning; 2–6 metadata workers
- UI thread never performs folder enumeration, hashing or metadata extraction
- Fast first/last-block fingerprint; full SHA-256 only for duplicate candidates
- SQLite WAL catalog and writes in batches of 100
- Paged queries (200 records) and recycled/virtualized WPF list items
- Lazy thumbnail queue limited to four concurrent decoders
- Search debounce, progress reporting and real cancellation
- Per-file error isolation and a scan error log
- Original photographs are never modified by catalog scanning

See [ARCHITECTURE.md](ARCHITECTURE.md) for boundaries and safety rules.

## No runtime installation required

Release builds are **self-contained and single-file**. Users do not install .NET, Python, SQLite or any package. NuGet/native components are bundled in the application output.

Supported downloads:

| Device | Download |
|---|---|
| Most Windows 10/11 PCs | `PhotoManager-Setup-2.0.0-win-x64.exe` |
| Portable x64 | `PhotoManager-2.0.0-win-x64-portable.zip` |
| Windows on ARM | `PhotoManager-2.0.0-win-arm64-portable.zip` |

WPF is Windows-only. These builds do not run on macOS or Linux. “No dependencies” means no separate runtime installation, not one binary that supports every operating system and processor.

## Current v2 features

- Responsive recursive scan with live counters, progress and Cancel
- Restart-safe SQLite catalog keyed by full file path
- JPG/PNG/WebP/BMP/GIF/TIFF plus cataloging of common HEIC/RAW extensions
- EXIF date, dimensions, camera and lens when metadata is readable
- Filename/path/notes search and tag filter
- Multi-selection tags, notes and 0–5 rating
- Missing-file check and consistent SQLite backup
- Persian RTL and English interface
- Self-contained x64 installer; portable x64 and ARM64 builds

Copy/move import is deliberately disabled during the migration. v2 first establishes a safe, non-destructive reference catalog. Transactional import will return only after journaling, copy verification and recovery are implemented.

## Build and test

Install the .NET 10 SDK only for development:

```powershell
dotnet test tests/PhotoManager.Tests/PhotoManager.Tests.csproj -c Release
./scripts/publish.ps1
```

The publish script creates self-contained artifacts under `artifacts/`. Inno Setup 6 is needed only to build the installer:

```powershell
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer-x64.iss
```

End users need none of these tools.

## Publish a GitHub pre-release

After tests pass on `main`:

```bash
git tag v2.0.0-beta.1
git push origin v2.0.0-beta.1
```

The release workflow tests, publishes x64/ARM64 self-contained executables, builds the x64 installer, calculates SHA-256 checksums, and creates a GitHub pre-release.

## Data and privacy

The application is offline and has no account, telemetry or cloud API. Data is stored under:

```text
%LOCALAPPDATA%\Mooli-web\PhotoManager\
```

The v2 catalog is separate from v1. Scanning only reads photos. Tags, ratings and notes stay in SQLite and do not alter original files.

## License

MIT © 2026 Mooli-web.
