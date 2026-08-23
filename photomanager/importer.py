from __future__ import annotations

import shutil
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Literal

from .database import Database
from .scanner import discover_images, read_metadata

ImportMode = Literal["copy", "move", "reference"]


@dataclass
class ImportResult:
    imported: int = 0
    duplicates: int = 0
    failed: list[str] = field(default_factory=list)


def unique_destination(folder: Path, filename: str) -> Path:
    candidate = folder / filename
    stem, suffix = Path(filename).stem, Path(filename).suffix
    counter = 2
    while candidate.exists():
        candidate = folder / f"{stem}_{counter}{suffix}"
        counter += 1
    return candidate


def import_folder(database: Database, source: str | Path, archive: str | Path | None = None,
                  mode: ImportMode = "reference", recursive: bool = True) -> ImportResult:
    result = ImportResult()
    for original in discover_images(source, recursive):
        try:
            metadata = read_metadata(original, include_hash=True)
            if database.find_by_hash(metadata["file_hash"]):
                result.duplicates += 1
                continue
            target = original
            if mode != "reference":
                if archive is None:
                    raise ValueError("An archive folder is required for copy or move mode")
                date_text = metadata.get("captured_at")
                try:
                    date = datetime.fromisoformat(date_text) if date_text else datetime.fromtimestamp(original.stat().st_mtime)
                except ValueError:
                    date = datetime.fromtimestamp(original.stat().st_mtime)
                folder = Path(archive).expanduser().resolve() / f"{date:%Y}" / f"{date:%Y-%m-%d}"
                folder.mkdir(parents=True, exist_ok=True)
                target = unique_destination(folder, original.name)
                if mode == "copy":
                    shutil.copy2(original, target)
                else:
                    shutil.move(original, target)
                metadata["path"], metadata["filename"] = str(target), target.name
            database.upsert_photo(metadata)
            result.imported += 1
        except Exception as exc:  # one bad file must not stop a complete import
            result.failed.append(f"{original}: {exc}")
    database.add_source(str(Path(archive if mode != "reference" else source).resolve()), True)
    return result
