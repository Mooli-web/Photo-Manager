using Microsoft.Win32;
using System.Windows;

namespace PhotoManager.Wpf.Services;

public sealed class DialogService : IDialogService
{
    public string? ChooseFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
    public string? SaveCatalog(string title)
    {
        var dialog = new SaveFileDialog { Title = title, Filter = "SQLite catalog (*.sqlite3)|*.sqlite3", FileName = $"photo-manager-{DateTime.Now:yyyyMMdd-HHmm}.sqlite3" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
    public void Info(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    public void Error(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
