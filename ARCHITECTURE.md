# Architecture and safety rules

## Dependency direction

```text
PhotoManager.Wpf ───────┐
                        ├─> PhotoManager.Application ─> PhotoManager.Domain
PhotoManager.Infrastructure ┘
```

- **Domain** contains immutable catalog entities and no framework dependency.
- **Application** owns use cases and interfaces. `PhotoScanner` is UI-independent.
- **Infrastructure** implements SQLite, fingerprints and metadata parsing.
- **WPF** composes services and displays paged view models. It never accesses SQLite or the filesystem directly.

## Scan pipeline

```text
Directory enumeration
    │ bounded Channel<string> (256)
    ▼
2–6 metadata/fingerprint workers
    │ bounded Channel<PhotoDraft> (128)
    ▼
Single batch writer (100 rows / transaction)
    ▼
SQLite WAL catalog
```

Bounded channels prevent an HDD from filling RAM faster than metadata can be processed. Cancellation flows through every channel, stream and database operation. A bad image is logged and does not terminate the scan.

## Duplicate strategy

1. Unchanged path + size + modification time is skipped.
2. Every changed/new file receives a quick fingerprint using file length and the first/last 64 KiB.
3. Only a matching size/fingerprint candidate triggers complete SHA-256 for both files.
4. Full hash is persisted and reused.

This avoids reading an entire archive on every scan while preserving full-hash confirmation for duplicate decisions.

## UI performance

- Queries return at most 200 photos per page.
- WPF uses a recycling `VirtualizingStackPanel`.
- Thumbnail decode is lazy, cached, downscaled during decode, and limited to four workers.
- Search waits 300 ms after typing and cancels the previous query.
- `Progress<T>` marshals small progress updates to the UI; file work remains off-thread.

## Safety invariants

- Catalog scan is read-only with respect to original photos.
- No delete or move feature exists in migration beta.
- SQLite uses WAL, transactions and a consistent backup API.
- Existing destination files will never be overwritten when transactional import is later introduced.
- A production Move flow must journal intent, copy, verify full hash, commit catalog, then delete source.
