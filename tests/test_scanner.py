from PIL import Image

from photomanager.scanner import discover_images, file_hash, read_metadata


def test_discovery_and_metadata(tmp_path):
    nested = tmp_path / "nested"
    nested.mkdir()
    image = nested / "sample.JPG"
    Image.new("RGB", (80, 60), "red").save(image)
    (tmp_path / "ignore.txt").write_text("no")
    assert discover_images(tmp_path) == [image.resolve()]
    metadata = read_metadata(image)
    assert (metadata["width"], metadata["height"]) == (80, 60)
    assert metadata["file_hash"] == file_hash(image)
