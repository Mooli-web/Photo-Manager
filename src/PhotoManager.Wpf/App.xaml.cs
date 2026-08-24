using System.IO;
using System.Windows;
using PhotoManager.Application.Services;
using PhotoManager.Infrastructure.Data;
using PhotoManager.Infrastructure.Files;
using PhotoManager.Infrastructure.Imaging;
using PhotoManager.Wpf.Services;
using PhotoManager.Wpf.ViewModels;

namespace PhotoManager.Wpf;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mooli-web", "PhotoManager");
            Directory.CreateDirectory(data);
            var catalog = new SqlitePhotoCatalog(Path.Combine(data, "catalog-v2.sqlite3"));
            await catalog.InitializeAsync();
            var scanner = new PhotoScanner(catalog, new FileFingerprintService(), new PhotoManager.Infrastructure.Imaging.ImageMetadataReader());
            var thumbnails = new ThumbnailService(Path.Combine(data, "thumbnails"));
            var vm = new MainWindowViewModel(catalog, scanner, thumbnails);
            var window = new MainWindow { DataContext = vm };
            MainWindow = window;
            window.Show();
            await vm.LoadFirstPageAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Photo Manager failed to start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
