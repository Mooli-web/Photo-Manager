namespace PhotoManager.Wpf.Services;

public interface IDialogService
{
    string? ChooseFolder(string title);
    string? SaveCatalog(string title);
    void Info(string title, string message);
    void Error(string title, string message);
}
