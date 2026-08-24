using System.Windows.Media;
using PhotoManager.Domain.Entities;
using PhotoManager.Wpf.Services;

namespace PhotoManager.Wpf.ViewModels;

public sealed class PhotoItemViewModel : ObservableObject
{
    private ImageSource? _thumbnail;
    public Photo Model { get; }
    public long Id => Model.Id;
    public string FileName => Model.FileName;
    public string Path => Model.Path;
    public string Details => $"{Model.Width?.ToString() ?? "—"} × {Model.Height?.ToString() ?? "—"}  ·  {Model.FileSize / 1048576d:F1} MB";
    public string Stars => Model.Rating == 0 ? "" : new string('★', Model.Rating);
    public ImageSource? Thumbnail { get => _thumbnail; private set => Set(ref _thumbnail, value); }

    public PhotoItemViewModel(Photo model, IThumbnailService thumbnails)
    {
        Model = model;
        _ = LoadThumbnailAsync(thumbnails);
    }

    private async Task LoadThumbnailAsync(IThumbnailService thumbnails)
    {
        try { Thumbnail = await thumbnails.GetAsync(Path); } catch (OperationCanceledException) { }
    }
}
