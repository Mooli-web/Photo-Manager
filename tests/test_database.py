from photomanager.database import Database


def photo(path="/archive/a.jpg", digest="abc"):
    return {"path": path, "filename": path.rsplit("/", 1)[-1], "file_hash": digest, "size": 10,
            "width": 100, "height": 50, "captured_at": None, "camera": None, "lens": None,
            "iso": None, "aperture": None, "shutter": None, "focal_length": None}


def test_photo_and_tags_are_unique():
    db = Database()
    photo_id = db.upsert_photo(photo())
    assert db.upsert_photo(photo()) == photo_id
    db.add_tags([photo_id], ["Nature", "Nature", " طبیعت "])
    assert db.tags_for_photo(photo_id) == ["Nature", "طبیعت"]
    assert len(db.photos(tag="nature")) == 1


def test_rating_and_search():
    db = Database()
    photo_id = db.upsert_photo(photo("/archive/sunset.jpg"))
    db.set_rating([photo_id], 4)
    assert len(db.photos(query="sunset", rating=4)) == 1
    assert db.photos(rating=5) == []


def test_same_name_different_paths_are_distinct():
    db = Database()
    first = db.upsert_photo(photo("/one/a.jpg", "one"))
    second = db.upsert_photo(photo("/two/a.jpg", "two"))
    assert first != second
