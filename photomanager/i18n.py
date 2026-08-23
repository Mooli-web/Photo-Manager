STRINGS = {
    "en": {
        "title": "Photo Manager", "library": "Library", "all_photos": "All photos",
        "add_folder": "Add folder", "import": "Import", "rescan": "Check missing files",
        "backup": "Backup catalog", "search": "Search filename, path or notes…", "tag_filter": "Filter by tag…",
        "rating": "Minimum rating", "photos": "photos", "details": "Details", "tags": "Tags",
        "add_tags": "Add tags", "remove_tag": "Remove tag", "notes": "Notes", "save": "Save",
        "open_folder": "Open containing folder", "mode_title": "Import method", "mode_question": "How should photos be added?",
        "reference": "Keep files where they are", "copy": "Copy into archive", "move": "Move into archive",
        "select_source": "Select the folder containing photos", "select_archive": "Select the archive destination",
        "done": "Import complete", "import_summary": "Imported: {imported}\nDuplicates skipped: {duplicates}\nFailed: {failed}",
        "error": "Error", "no_selection": "Select one or more photos first.", "tag_prompt": "Comma-separated tags:",
        "missing_summary": "{count} missing files found.", "language": "فارسی", "theme": "Toggle theme",
        "filename": "File", "path": "Path", "captured": "Captured", "camera": "Camera", "lens": "Lens",
        "dimensions": "Dimensions", "size": "Size", "rating_label": "Rating", "untagged": "No tags",
    },
    "fa": {
        "title": "مدیریت آرشیو عکس", "library": "کتابخانه", "all_photos": "همه عکس‌ها",
        "add_folder": "افزودن پوشه", "import": "ورود عکس‌ها", "rescan": "بررسی فایل‌های گم‌شده",
        "backup": "پشتیبان‌گیری از کاتالوگ", "search": "جست‌وجوی نام، مسیر یا یادداشت…", "tag_filter": "فیلتر بر اساس تگ…",
        "rating": "حداقل امتیاز", "photos": "عکس", "details": "جزئیات", "tags": "تگ‌ها",
        "add_tags": "افزودن تگ", "remove_tag": "حذف تگ", "notes": "یادداشت", "save": "ذخیره",
        "open_folder": "بازکردن محل فایل", "mode_title": "روش ورود", "mode_question": "عکس‌ها چگونه اضافه شوند؟",
        "reference": "فایل‌ها در محل فعلی بمانند", "copy": "کپی داخل آرشیو", "move": "انتقال به آرشیو",
        "select_source": "پوشه حاوی عکس‌ها را انتخاب کنید", "select_archive": "مقصد آرشیو را انتخاب کنید",
        "done": "ورود عکس‌ها کامل شد", "import_summary": "واردشده: {imported}\nتکراری ردشده: {duplicates}\nناموفق: {failed}",
        "error": "خطا", "no_selection": "ابتدا یک یا چند عکس را انتخاب کنید.", "tag_prompt": "تگ‌ها را با ویرگول جدا کنید:",
        "missing_summary": "{count} فایل گم‌شده پیدا شد.", "language": "English", "theme": "تغییر پوسته",
        "filename": "فایل", "path": "مسیر", "captured": "زمان ثبت", "camera": "دوربین", "lens": "لنز",
        "dimensions": "ابعاد", "size": "حجم", "rating_label": "امتیاز", "untagged": "بدون تگ",
    },
}


def tr(language: str, key: str) -> str:
    return STRINGS.get(language, STRINGS["en"]).get(key, key)
