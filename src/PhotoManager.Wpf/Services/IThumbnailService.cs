using System.Windows.Media;

namespace PhotoManager.Wpf.Services;

public interface IThumbnailService
{
    Task<ImageSource?> GetAsync(string sourcePath, CancellationToken cancellationToken = default);
}
