from pathlib import Path

from PIL import Image

from photomanager.database import Database
from photomanager.importer import import_folder


def test_copy_import_and_duplicate_detection(tmp_path):
    source, archive = tmp_path / "source", tmp_path / "archive"
    source.mkdir()
    image = source / "photo.jpg"
    Image.new("RGB", (20, 10), "blue").save(image)
    db = Database()
    first = import_folder(db, source, archive, "copy")
    assert first.imported == 1
    assert Path(db.photos()[0]["path"]).exists()
    second = import_folder(db, source, archive, "copy")
    assert second.duplicates == 1
