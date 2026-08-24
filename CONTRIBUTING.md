# Contributing / مشارکت

## English

1. Open an issue before a large change.
2. Never commit personal photos, catalogs, thumbnails, `bin`, `obj`, or release artifacts.
3. Keep dependency direction documented in `ARCHITECTURE.md`.
4. File I/O, hashing, metadata and database work must never execute on the WPF dispatcher thread.
5. Every long operation must accept `CancellationToken`, report progress, isolate per-file errors and have a bounded queue.
6. Add tests and run:

```powershell
dotnet test tests/PhotoManager.Tests/PhotoManager.Tests.csproj -c Release
```

Visible UI text must be provided in Persian and English. Include screenshots and performance observations with UI or scanner pull requests.

## فارسی

۱. پیش از تغییر بزرگ یک Issue بسازید.  
۲. عکس، کاتالوگ، Thumbnail، پوشه‌های `bin` و `obj` یا خروجی Release را Commit نکنید.  
۳. جهت وابستگی‌های `ARCHITECTURE.md` را حفظ کنید.  
۴. فایل، هش، Metadata و دیتابیس نباید روی Thread رابط WPF اجرا شوند.  
۵. عملیات طولانی باید توقف‌پذیر، دارای پیشرفت، صف محدود و مدیریت مستقل خطای هر فایل باشد.  
۶. تست اضافه کنید و دستور بالا را اجرا کنید. متن قابل‌مشاهده باید فارسی و انگلیسی داشته باشد.
