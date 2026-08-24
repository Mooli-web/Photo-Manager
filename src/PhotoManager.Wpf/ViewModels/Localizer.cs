using System.Windows;

namespace PhotoManager.Wpf.ViewModels;

public sealed class Localizer : ObservableObject
{
    private bool _persian = true;
    public bool Persian { get => _persian; set { if (Set(ref _persian, value)) RaiseAll(); } }
    public FlowDirection Flow => Persian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    public string Title => T("مدیریت آرشیو عکس", "Photo Manager");
    public string AddFolder => T("افزودن پوشه", "Add folder");
    public string Cancel => T("توقف", "Cancel");
    public string Search => T("جست‌وجوی نام، مسیر یا یادداشت…", "Search name, path or notes…");
    public string TagFilter => T("فیلتر تگ…", "Filter tag…");
    public string Rating => T("حداقل امتیاز", "Minimum rating");
    public string Progress => T("پیشرفت", "Progress");
    public string Discovered => T("پیداشده", "Discovered");
    public string Processed => T("بررسی‌شده", "Processed");
    public string Added => T("ثبت‌شده", "Added");
    public string Failed => T("خطا", "Failed");
    public string LoadMore => T("نمایش موارد بیشتر", "Load more");
    public string AddTags => T("افزودن تگ", "Add tag");
    public string RemoveTag => T("حذف تگ", "Remove tag");
    public string TagInput => T("تگ را بنویسید و Enter بزنید", "Type a tag and press Enter");
    public string CurrentTags => T("تگ‌های این عکس", "Current tags");
    public string PreviousTags => T("تگ‌های قبلی", "Existing tags");
    public string TagInputHelp => T("هر بار یک تگ اضافه می‌شود. با کلیک روی تگ‌های قبلی هم می‌توانید همان تگ را اضافه کنید.", "Add one tag at a time. Click an existing tag to reuse it.");
    public string SaveNotes => T("ذخیره یادداشت", "Save notes");
    public string Notes => T("یادداشت", "Notes");
    public string Backup => T("پشتیبان‌گیری", "Backup catalog");
    public string CheckMissing => T("بررسی فایل‌های گم‌شده", "Check missing files");
    public string Language => Persian ? "English" : "فارسی";
    public string SelectFolder => T("پوشه عکس‌ها را انتخاب کنید", "Select the photo folder");
    public string Complete => T("اسکن کامل شد", "Scan completed");
    public string SelectPhoto => T("یک یا چند عکس را انتخاب کنید", "Select one or more photos");
    public string Photos => T("عکس", "photos");
    private string T(string fa, string en) => Persian ? fa : en;
    private void RaiseAll() { Raise(string.Empty); }
}
