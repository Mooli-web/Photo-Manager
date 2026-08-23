from __future__ import annotations

import sys
from pathlib import Path

from PyQt6.QtCore import QLocale, QStandardPaths, QTranslator
from PyQt6.QtGui import QIcon
from PyQt6.QtWidgets import QApplication

from . import __version__
from .database import Database
from .ui.main_window import MainWindow


def app_data_dir() -> Path:
    path = Path(QStandardPaths.writableLocation(QStandardPaths.StandardLocation.AppDataLocation))
    path.mkdir(parents=True, exist_ok=True)
    return path


def main() -> int:
    app = QApplication(sys.argv)
    app.setApplicationName("Photo Manager")
    app.setApplicationVersion(__version__)
    app.setOrganizationName("Mooli-web")
    app.setStyle("Fusion")

    icon = Path(__file__).resolve().parent.parent / "assets" / "icon.ico"
    if icon.exists():
        app.setWindowIcon(QIcon(str(icon)))

    data_dir = app_data_dir()
    database = Database(data_dir / "catalog.sqlite3")
    window = MainWindow(database=database, data_dir=data_dir)
    window.show()
    code = app.exec()
    database.close()
    return code
