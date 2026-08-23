from __future__ import annotations

import os
import shutil
import subprocess
from pathlib import Path

from PyQt6.QtCore import QSettings, QSize, Qt, QUrl
from PyQt6.QtGui import QAction, QDesktopServices, QKeySequence, QPixmap
from PyQt6.QtWidgets import (
    QApplication, QComboBox, QFileDialog, QFormLayout, QFrame, QHBoxLayout, QInputDialog,
    QLabel, QLineEdit, QListWidget, QListWidgetItem, QMainWindow, QMessageBox, QPlainTextEdit,
    QPushButton, QSplitter, QToolBar, QVBoxLayout, QWidget, QAbstractItemView,
)

from ..database import Database
from ..i18n import tr
from ..importer import import_folder
from ..thumbnails import ThumbnailCache

DARK_STYLE = """
QWidget { background:#171a20; color:#e7eaf0; }
QLineEdit, QPlainTextEdit, QComboBox, QListWidget { background:#20242b; border:1px solid #343a46; border-radius:6px; padding:5px; }
QPushButton { background:#2c64d8; border:0; border-radius:6px; padding:7px 12px; }
QPushButton:hover { background:#3975ef; }
QToolBar { border:0; spacing:5px; }
QStatusBar { border-top:1px solid #343a46; }
"""


class MainWindow(QMainWindow):
    def __init__(self, database: Database, data_dir: Path) -> None:
        super().__init__()
        self.db, self.data_dir = database, data_dir
        self.settings = QSettings("Mooli-web", "PhotoManager")
        self.language = self.settings.value("language", "fa")
        self.dark = self.settings.value("dark", True, type=bool)
        self.cache = ThumbnailCache(data_dir / "thumbnails")
        self._building = False
        self.resize(1280, 780)
        self._build_ui()
        self.apply_language()
        self.apply_theme()
        self.refresh()

    def t(self, key: str) -> str:
        return tr(self.language, key)

    def _build_ui(self) -> None:
        toolbar = QToolBar()
        toolbar.setMovable(False)
        self.addToolBar(toolbar)
        self.add_folder_action = QAction(self)
        self.add_folder_action.triggered.connect(self.add_folder)
        toolbar.addAction(self.add_folder_action)
        self.import_action = QAction(self)
        self.import_action.triggered.connect(self.import_photos)
        toolbar.addAction(self.import_action)
        self.rescan_action = QAction(self)
        self.rescan_action.triggered.connect(self.check_missing)
        toolbar.addAction(self.rescan_action)
        self.backup_action = QAction(self)
        self.backup_action.triggered.connect(self.backup_catalog)
        toolbar.addAction(self.backup_action)
        toolbar.addSeparator()
        self.theme_action = QAction(self)
        self.theme_action.triggered.connect(self.toggle_theme)
        toolbar.addAction(self.theme_action)
        self.language_action = QAction(self)
        self.language_action.triggered.connect(self.toggle_language)
        toolbar.addAction(self.language_action)

        root = QWidget()
        outer = QVBoxLayout(root)
        filters = QHBoxLayout()
        self.search = QLineEdit()
        self.search.setClearButtonEnabled(True)
        self.search.textChanged.connect(self.refresh)
        filters.addWidget(self.search, 3)
        self.tag_filter = QLineEdit()
        self.tag_filter.setClearButtonEnabled(True)
        self.tag_filter.textChanged.connect(self.refresh)
        filters.addWidget(self.tag_filter, 2)
        self.rating_filter = QComboBox()
        self.rating_filter.addItems(["0", "1 ★", "2 ★", "3 ★", "4 ★", "5 ★"])
        self.rating_filter.currentIndexChanged.connect(self.refresh)
        filters.addWidget(self.rating_filter)
        self.count_label = QLabel()
        filters.addWidget(self.count_label)
        outer.addLayout(filters)

        splitter = QSplitter()
        self.grid = QListWidget()
        self.grid.setViewMode(QListWidget.ViewMode.IconMode)
        self.grid.setResizeMode(QListWidget.ResizeMode.Adjust)
        self.grid.setMovement(QListWidget.Movement.Static)
        self.grid.setIconSize(QSize(180, 180))
        self.grid.setGridSize(QSize(205, 225))
        self.grid.setSpacing(6)
        self.grid.setSelectionMode(QAbstractItemView.SelectionMode.ExtendedSelection)
        self.grid.itemSelectionChanged.connect(self.show_selected)
        self.grid.itemDoubleClicked.connect(self.open_file)
        splitter.addWidget(self.grid)

        side = QFrame()
        side.setMinimumWidth(300)
        side.setMaximumWidth(420)
        side_layout = QVBoxLayout(side)
        self.preview = QLabel()
        self.preview.setAlignment(Qt.AlignmentFlag.AlignCenter)
        self.preview.setMinimumHeight(220)
        self.preview.setStyleSheet("border:1px solid #343a46; border-radius:8px;")
        side_layout.addWidget(self.preview)
        self.details_title = QLabel()
        self.details_title.setStyleSheet("font-size:16px;font-weight:600")
        side_layout.addWidget(self.details_title)
        self.details = QLabel()
        self.details.setTextInteractionFlags(Qt.TextInteractionFlag.TextSelectableByMouse)
        self.details.setWordWrap(True)
        side_layout.addWidget(self.details)
        self.tags_title = QLabel()
        self.tags_title.setStyleSheet("font-weight:600")
        side_layout.addWidget(self.tags_title)
        self.tags_value = QLabel()
        self.tags_value.setWordWrap(True)
        side_layout.addWidget(self.tags_value)
        tag_buttons = QHBoxLayout()
        self.add_tags_button = QPushButton()
        self.add_tags_button.clicked.connect(self.add_tags)
        tag_buttons.addWidget(self.add_tags_button)
        self.remove_tag_button = QPushButton()
        self.remove_tag_button.clicked.connect(self.remove_tag)
        tag_buttons.addWidget(self.remove_tag_button)
        side_layout.addLayout(tag_buttons)
        rating_line = QHBoxLayout()
        self.rating_title = QLabel()
        rating_line.addWidget(self.rating_title)
        self.rating = QComboBox()
        self.rating.addItems(["0", "1 ★", "2 ★", "3 ★", "4 ★", "5 ★"])
        self.rating.activated.connect(self.update_rating)
        rating_line.addWidget(self.rating)
        side_layout.addLayout(rating_line)
        self.notes_title = QLabel()
        side_layout.addWidget(self.notes_title)
        self.notes = QPlainTextEdit()
        self.notes.setMaximumHeight(100)
        side_layout.addWidget(self.notes)
        self.save_notes_button = QPushButton()
        self.save_notes_button.clicked.connect(self.save_notes)
        side_layout.addWidget(self.save_notes_button)
        self.open_folder_button = QPushButton()
        self.open_folder_button.clicked.connect(self.open_folder)
        side_layout.addWidget(self.open_folder_button)
        side_layout.addStretch()
        splitter.addWidget(side)
        splitter.setStretchFactor(0, 1)
        outer.addWidget(splitter)
        self.setCentralWidget(root)

        refresh_shortcut = QAction(self)
        refresh_shortcut.setShortcut(QKeySequence.StandardKey.Refresh)
        refresh_shortcut.triggered.connect(self.refresh)
        self.addAction(refresh_shortcut)

    def apply_language(self) -> None:
        rtl = self.language == "fa"
        QApplication.instance().setLayoutDirection(Qt.LayoutDirection.RightToLeft if rtl else Qt.LayoutDirection.LeftToRight)
        self.setWindowTitle(self.t("title"))
        self.add_folder_action.setText(self.t("add_folder"))
        self.import_action.setText(self.t("import"))
        self.rescan_action.setText(self.t("rescan"))
        self.backup_action.setText(self.t("backup"))
        self.theme_action.setText(self.t("theme"))
        self.language_action.setText(self.t("language"))
        self.search.setPlaceholderText(self.t("search"))
        self.tag_filter.setPlaceholderText(self.t("tag_filter"))
        self.rating_filter.setToolTip(self.t("rating"))
        self.details_title.setText(self.t("details"))
        self.tags_title.setText(self.t("tags"))
        self.add_tags_button.setText(self.t("add_tags"))
        self.remove_tag_button.setText(self.t("remove_tag"))
        self.rating_title.setText(self.t("rating_label"))
        self.notes_title.setText(self.t("notes"))
        self.save_notes_button.setText(self.t("save"))
        self.open_folder_button.setText(self.t("open_folder"))
        self.show_selected()

    def apply_theme(self) -> None:
        QApplication.instance().setStyleSheet(DARK_STYLE if self.dark else "")

    def toggle_language(self) -> None:
        self.language = "en" if self.language == "fa" else "fa"
        self.settings.setValue("language", self.language)
        self.apply_language()
        self.refresh()

    def toggle_theme(self) -> None:
        self.dark = not self.dark
        self.settings.setValue("dark", self.dark)
        self.apply_theme()

    def selected_ids(self) -> list[int]:
        return [int(item.data(Qt.ItemDataRole.UserRole)) for item in self.grid.selectedItems()]

    def refresh(self) -> None:
        if self._building:
            return
        selected = set(self.selected_ids())
        rows = self.db.photos(self.search.text().strip(), self.tag_filter.text().strip(), self.rating_filter.currentIndex())
        self._building = True
        self.grid.clear()
        for row in rows:
            text = row["filename"] + ("\n" + "★" * row["rating"] if row["rating"] else "")
            if row["missing"]:
                text = "⚠ " + text
            item = QListWidgetItem(self.cache.icon(row["path"]), text)
            item.setData(Qt.ItemDataRole.UserRole, row["id"])
            item.setToolTip(row["path"])
            self.grid.addItem(item)
            if row["id"] in selected:
                item.setSelected(True)
        self._building = False
        self.count_label.setText(f"{len(rows)} {self.t('photos')}")
        self.show_selected()

    def show_selected(self) -> None:
        ids = self.selected_ids()
        enabled = bool(ids)
        for widget in (self.add_tags_button, self.remove_tag_button, self.rating, self.open_folder_button):
            widget.setEnabled(enabled)
        if not ids:
            self.preview.clear()
            self.details.clear()
            self.tags_value.setText(self.t("untagged"))
            self.notes.clear()
            return
        row = self.db.get_photo(ids[0])
        if not row:
            return
        pixmap = QPixmap(row["path"])
        if not pixmap.isNull():
            self.preview.setPixmap(pixmap.scaled(360, 260, Qt.AspectRatioMode.KeepAspectRatio, Qt.TransformationMode.SmoothTransformation))
        else:
            self.preview.setText("⚠")
        dimensions = f"{row['width'] or '—'} × {row['height'] or '—'}"
        mb = row["size"] / 1048576
        details = [
            (self.t("filename"), row["filename"]), (self.t("path"), row["path"]),
            (self.t("captured"), row["captured_at"] or "—"), (self.t("camera"), row["camera"] or "—"),
            (self.t("lens"), row["lens"] or "—"), (self.t("dimensions"), dimensions),
            (self.t("size"), f"{mb:.2f} MB"),
        ]
        self.details.setText("\n".join(f"{label}: {value}" for label, value in details))
        tags = self.db.tags_for_photo(ids[0])
        self.tags_value.setText("، ".join(tags) if tags else self.t("untagged"))
        self.rating.blockSignals(True)
        self.rating.setCurrentIndex(row["rating"])
        self.rating.blockSignals(False)
        self.notes.setPlainText(row["notes"])

    def add_folder(self) -> None:
        folder = QFileDialog.getExistingDirectory(self, self.t("select_source"))
        if folder:
            self._run_import(folder, "reference", None)

    def import_photos(self) -> None:
        source = QFileDialog.getExistingDirectory(self, self.t("select_source"))
        if not source:
            return
        box = QMessageBox(self)
        box.setWindowTitle(self.t("mode_title"))
        box.setText(self.t("mode_question"))
        reference = box.addButton(self.t("reference"), QMessageBox.ButtonRole.ActionRole)
        copy = box.addButton(self.t("copy"), QMessageBox.ButtonRole.ActionRole)
        move = box.addButton(self.t("move"), QMessageBox.ButtonRole.DestructiveRole)
        box.addButton(QMessageBox.StandardButton.Cancel)
        box.exec()
        clicked = box.clickedButton()
        if clicked == reference:
            self._run_import(source, "reference", None)
        elif clicked in (copy, move):
            archive = QFileDialog.getExistingDirectory(self, self.t("select_archive"))
            if archive:
                self._run_import(source, "copy" if clicked == copy else "move", archive)

    def _run_import(self, source: str, mode: str, archive: str | None) -> None:
        QApplication.setOverrideCursor(Qt.CursorShape.WaitCursor)
        try:
            result = import_folder(self.db, source, archive, mode)  # type: ignore[arg-type]
            QMessageBox.information(self, self.t("done"), self.t("import_summary").format(
                imported=result.imported, duplicates=result.duplicates, failed=len(result.failed)))
            if result.failed:
                (self.data_dir / "last-import-errors.txt").write_text("\n".join(result.failed), encoding="utf-8")
            self.refresh()
        except Exception as exc:
            QMessageBox.critical(self, self.t("error"), str(exc))
        finally:
            QApplication.restoreOverrideCursor()

    def add_tags(self) -> None:
        ids = self.selected_ids()
        if not ids:
            return
        value, ok = QInputDialog.getText(self, self.t("add_tags"), self.t("tag_prompt"))
        if ok:
            self.db.add_tags(ids, value.replace("،", ",").split(","))
            self.show_selected()

    def remove_tag(self) -> None:
        ids = self.selected_ids()
        if not ids:
            return
        available = sorted({tag for photo_id in ids for tag in self.db.tags_for_photo(photo_id)}, key=str.casefold)
        if not available:
            return
        value, ok = QInputDialog.getItem(self, self.t("remove_tag"), self.t("tags"), available, 0, False)
        if ok:
            self.db.remove_tag(ids, value)
            self.show_selected()

    def update_rating(self, index: int) -> None:
        ids = self.selected_ids()
        if ids:
            self.db.set_rating(ids, index)
            self.refresh()

    def save_notes(self) -> None:
        ids = self.selected_ids()
        if len(ids) == 1:
            self.db.set_notes(ids[0], self.notes.toPlainText().strip())
            self.statusBar().showMessage(self.t("save"), 2000)

    def open_file(self, item: QListWidgetItem) -> None:
        row = self.db.get_photo(int(item.data(Qt.ItemDataRole.UserRole)))
        if row and Path(row["path"]).exists():
            QDesktopServices.openUrl(QUrl.fromLocalFile(row["path"]))

    def open_folder(self) -> None:
        ids = self.selected_ids()
        if not ids:
            return
        row = self.db.get_photo(ids[0])
        if not row:
            return
        path = Path(row["path"])
        if os.name == "nt" and path.exists():
            subprocess.Popen(["explorer", "/select,", str(path)])
        else:
            QDesktopServices.openUrl(QUrl.fromLocalFile(str(path.parent)))

    def check_missing(self) -> None:
        count = self.db.mark_missing()
        QMessageBox.information(self, self.t("rescan"), self.t("missing_summary").format(count=count))
        self.refresh()

    def backup_catalog(self) -> None:
        if not self.db.path:
            return
        filename, _ = QFileDialog.getSaveFileName(self, self.t("backup"), "photo-manager-catalog-backup.sqlite3", "SQLite (*.sqlite3)")
        if filename:
            self.db.conn.execute("PRAGMA wal_checkpoint(FULL)")
            destination = Path(filename)
            backup = __import__("sqlite3").connect(destination)
            try:
                self.db.conn.backup(backup)
            finally:
                backup.close()
            self.statusBar().showMessage(str(destination), 4000)

    def closeEvent(self, event) -> None:  # noqa: N802
        self.settings.setValue("geometry", self.saveGeometry())
        super().closeEvent(event)
