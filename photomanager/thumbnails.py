from __future__ import annotations

import hashlib
from pathlib import Path

from PIL import Image, ImageOps
from PyQt6.QtGui import QIcon, QPixmap


class ThumbnailCache:
    def __init__(self, folder: Path, size: int = 256) -> None:
        self.folder, self.size = folder, size
        folder.mkdir(parents=True, exist_ok=True)

    def path_for(self, source: str) -> Path:
        key = hashlib.sha1(f"{source}:{Path(source).stat().st_mtime_ns if Path(source).exists() else 0}".encode()).hexdigest()
        return self.folder / f"{key}.jpg"

    def icon(self, source: str) -> QIcon:
        cached = self.path_for(source)
        if not cached.exists() and Path(source).is_file():
            try:
                with Image.open(source) as image:
                    image = ImageOps.exif_transpose(image).convert("RGB")
                    image.thumbnail((self.size, self.size))
                    canvas = Image.new("RGB", (self.size, self.size), "#20242b")
                    canvas.paste(image, ((self.size-image.width)//2, (self.size-image.height)//2))
                    canvas.save(cached, "JPEG", quality=82)
            except (OSError, ValueError):
                return QIcon()
        return QIcon(QPixmap(str(cached))) if cached.exists() else QIcon()
