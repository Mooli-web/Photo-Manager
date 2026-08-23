from __future__ import annotations

import hashlib
from datetime import datetime
from pathlib import Path
from typing import Callable, Iterable

from PIL import ExifTags, Image, UnidentifiedImageError

SUPPORTED_EXTENSIONS = {
    ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tif", ".tiff",
    ".heic", ".heif", ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf",
}


def discover_images(folder: str | Path, recursive: bool = True) -> list[Path]:
    root = Path(folder)
    iterator = root.rglob("*") if recursive else root.glob("*")
    return sorted((p.resolve() for p in iterator if p.is_file() and p.suffix.lower() in SUPPORTED_EXTENSIONS), key=lambda p: str(p).casefold())


def file_hash(path: str | Path, chunk_size: int = 1024 * 1024) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(chunk_size), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _text(value: object) -> str | None:
    if value is None:
        return None
    if isinstance(value, bytes):
        return value.decode(errors="replace").strip("\x00 ")
    return str(value).strip()


def read_metadata(path: str | Path, include_hash: bool = True) -> dict:
    photo = Path(path).resolve()
    metadata: dict = {
        "path": str(photo), "filename": photo.name, "size": photo.stat().st_size,
        "file_hash": file_hash(photo) if include_hash else None,
        "width": None, "height": None, "captured_at": None, "camera": None, "lens": None,
        "iso": None, "aperture": None, "shutter": None, "focal_length": None,
    }
    try:
        with Image.open(photo) as image:
            metadata["width"], metadata["height"] = image.size
            raw = image.getexif()
            exif = {ExifTags.TAGS.get(k, str(k)): v for k, v in raw.items()}
            date = _text(exif.get("DateTimeOriginal") or exif.get("DateTime"))
            if date:
                try:
                    metadata["captured_at"] = datetime.strptime(date, "%Y:%m:%d %H:%M:%S").isoformat(sep=" ")
                except ValueError:
                    metadata["captured_at"] = date
            make, model = _text(exif.get("Make")), _text(exif.get("Model"))
            metadata["camera"] = " ".join(filter(None, [make, model])) or None
            metadata["lens"] = _text(exif.get("LensModel"))
            metadata["iso"] = exif.get("ISOSpeedRatings")
            metadata["aperture"] = _text(exif.get("FNumber"))
            metadata["shutter"] = _text(exif.get("ExposureTime"))
            metadata["focal_length"] = _text(exif.get("FocalLength"))
    except (UnidentifiedImageError, OSError, ValueError):
        pass
    return metadata
