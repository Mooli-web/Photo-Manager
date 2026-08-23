from __future__ import annotations

import sqlite3
from contextlib import contextmanager
from pathlib import Path
from typing import Iterable, Iterator

SCHEMA_VERSION = 1


class Database:
    """SQLite catalog. The original photo files are never modified."""

    def __init__(self, path: str | Path = ":memory:") -> None:
        self.path = Path(path) if str(path) != ":memory:" else None
        if self.path:
            self.path.parent.mkdir(parents=True, exist_ok=True)
        self.conn = sqlite3.connect(str(path))
        self.conn.row_factory = sqlite3.Row
        self.conn.execute("PRAGMA foreign_keys = ON")
        self.conn.execute("PRAGMA journal_mode = WAL")
        self._create_schema()

    def _create_schema(self) -> None:
        self.conn.executescript(
            """
            CREATE TABLE IF NOT EXISTS photos (
                id INTEGER PRIMARY KEY,
                path TEXT NOT NULL UNIQUE COLLATE NOCASE,
                filename TEXT NOT NULL,
                file_hash TEXT,
                size INTEGER NOT NULL DEFAULT 0,
                width INTEGER,
                height INTEGER,
                captured_at TEXT,
                camera TEXT,
                lens TEXT,
                iso INTEGER,
                aperture TEXT,
                shutter TEXT,
                focal_length TEXT,
                rating INTEGER NOT NULL DEFAULT 0 CHECK(rating BETWEEN 0 AND 5),
                favorite INTEGER NOT NULL DEFAULT 0,
                notes TEXT NOT NULL DEFAULT '',
                missing INTEGER NOT NULL DEFAULT 0,
                imported_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_photos_filename ON photos(filename);
            CREATE INDEX IF NOT EXISTS idx_photos_hash ON photos(file_hash);
            CREATE INDEX IF NOT EXISTS idx_photos_captured ON photos(captured_at);

            CREATE TABLE IF NOT EXISTS tags (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                color TEXT NOT NULL DEFAULT '#4f8cff'
            );
            CREATE TABLE IF NOT EXISTS photo_tags (
                photo_id INTEGER NOT NULL REFERENCES photos(id) ON DELETE CASCADE,
                tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                PRIMARY KEY(photo_id, tag_id)
            );
            CREATE TABLE IF NOT EXISTS albums (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE TABLE IF NOT EXISTS album_photos (
                album_id INTEGER NOT NULL REFERENCES albums(id) ON DELETE CASCADE,
                photo_id INTEGER NOT NULL REFERENCES photos(id) ON DELETE CASCADE,
                PRIMARY KEY(album_id, photo_id)
            );
            CREATE TABLE IF NOT EXISTS sources (
                id INTEGER PRIMARY KEY,
                path TEXT NOT NULL UNIQUE COLLATE NOCASE,
                recursive INTEGER NOT NULL DEFAULT 1,
                added_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            PRAGMA user_version = 1;
            """
        )
        self.conn.commit()

    @contextmanager
    def transaction(self) -> Iterator[sqlite3.Connection]:
        try:
            yield self.conn
            self.conn.commit()
        except Exception:
            self.conn.rollback()
            raise

    def upsert_photo(self, metadata: dict) -> int:
        fields = [
            "path", "filename", "file_hash", "size", "width", "height",
            "captured_at", "camera", "lens", "iso", "aperture", "shutter", "focal_length",
        ]
        values = [metadata.get(field) for field in fields]
        self.conn.execute(
            f"""INSERT INTO photos ({','.join(fields)}) VALUES ({','.join('?' for _ in fields)})
            ON CONFLICT(path) DO UPDATE SET
                filename=excluded.filename, file_hash=COALESCE(excluded.file_hash, photos.file_hash),
                size=excluded.size, width=excluded.width, height=excluded.height,
                captured_at=excluded.captured_at, camera=excluded.camera, lens=excluded.lens,
                iso=excluded.iso, aperture=excluded.aperture, shutter=excluded.shutter,
                focal_length=excluded.focal_length, missing=0""",
            values,
        )
        self.conn.commit()
        row = self.conn.execute("SELECT id FROM photos WHERE path=?", (metadata["path"],)).fetchone()
        return int(row[0])

    def add_source(self, path: str, recursive: bool = True) -> None:
        self.conn.execute(
            "INSERT INTO sources(path, recursive) VALUES(?, ?) ON CONFLICT(path) DO UPDATE SET recursive=excluded.recursive",
            (path, int(recursive)),
        )
        self.conn.commit()

    def list_sources(self) -> list[sqlite3.Row]:
        return list(self.conn.execute("SELECT * FROM sources ORDER BY path"))

    def find_by_hash(self, digest: str) -> sqlite3.Row | None:
        return self.conn.execute("SELECT * FROM photos WHERE file_hash=? LIMIT 1", (digest,)).fetchone()

    def photos(self, query: str = "", tag: str = "", rating: int = 0, missing: bool | None = None) -> list[sqlite3.Row]:
        sql = "SELECT DISTINCT p.* FROM photos p"
        params: list[object] = []
        where: list[str] = []
        if tag:
            sql += " JOIN photo_tags pt ON pt.photo_id=p.id JOIN tags t ON t.id=pt.tag_id"
            where.append("t.name LIKE ?")
            params.append(f"%{tag}%")
        if query:
            where.append("(p.filename LIKE ? OR p.path LIKE ? OR p.notes LIKE ?)")
            params.extend([f"%{query}%"] * 3)
        if rating:
            where.append("p.rating >= ?")
            params.append(rating)
        if missing is not None:
            where.append("p.missing = ?")
            params.append(int(missing))
        if where:
            sql += " WHERE " + " AND ".join(where)
        sql += " ORDER BY COALESCE(p.captured_at, p.imported_at) DESC, p.filename"
        return list(self.conn.execute(sql, params))

    def get_photo(self, photo_id: int) -> sqlite3.Row | None:
        return self.conn.execute("SELECT * FROM photos WHERE id=?", (photo_id,)).fetchone()

    def set_rating(self, photo_ids: Iterable[int], rating: int) -> None:
        self.conn.executemany("UPDATE photos SET rating=? WHERE id=?", ((rating, i) for i in photo_ids))
        self.conn.commit()

    def set_notes(self, photo_id: int, notes: str) -> None:
        self.conn.execute("UPDATE photos SET notes=? WHERE id=?", (notes, photo_id))
        self.conn.commit()

    def tags_for_photo(self, photo_id: int) -> list[str]:
        rows = self.conn.execute(
            "SELECT t.name FROM tags t JOIN photo_tags pt ON pt.tag_id=t.id WHERE pt.photo_id=? ORDER BY t.name",
            (photo_id,),
        )
        return [str(r[0]) for r in rows]

    def all_tags(self) -> list[sqlite3.Row]:
        return list(self.conn.execute(
            "SELECT t.*, COUNT(pt.photo_id) AS photo_count FROM tags t LEFT JOIN photo_tags pt ON pt.tag_id=t.id GROUP BY t.id ORDER BY t.name"
        ))

    def add_tags(self, photo_ids: Iterable[int], tags: Iterable[str]) -> None:
        clean = sorted({tag.strip() for tag in tags if tag.strip()}, key=str.casefold)
        ids = list(photo_ids)
        with self.transaction() as conn:
            for name in clean:
                conn.execute("INSERT OR IGNORE INTO tags(name) VALUES(?)", (name,))
                tag_id = conn.execute("SELECT id FROM tags WHERE name=? COLLATE NOCASE", (name,)).fetchone()[0]
                conn.executemany("INSERT OR IGNORE INTO photo_tags(photo_id, tag_id) VALUES(?, ?)", ((p, tag_id) for p in ids))

    def remove_tag(self, photo_ids: Iterable[int], tag: str) -> None:
        self.conn.executemany(
            "DELETE FROM photo_tags WHERE photo_id=? AND tag_id=(SELECT id FROM tags WHERE name=? COLLATE NOCASE)",
            ((p, tag) for p in photo_ids),
        )
        self.conn.execute("DELETE FROM tags WHERE id NOT IN (SELECT tag_id FROM photo_tags)")
        self.conn.commit()

    def mark_missing(self) -> int:
        rows = self.conn.execute("SELECT id, path FROM photos").fetchall()
        changed = 0
        with self.transaction() as conn:
            for row in rows:
                value = int(not Path(row["path"]).is_file())
                conn.execute("UPDATE photos SET missing=? WHERE id=?", (value, row["id"]))
                changed += value
        return changed

    def close(self) -> None:
        self.conn.close()
