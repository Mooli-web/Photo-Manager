using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using PhotoManager.Application.Services;
using PhotoManager.Infrastructure.Data;
using PhotoManager.Infrastructure.Files;
using PhotoManager.Infrastructure.Imaging;
using PhotoManager.Wpf;
using PhotoManager.Wpf.Services;
using PhotoManager.Wpf.ViewModels;

namespace PhotoManager.Wpf.Smoke;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PhotoManagerWpfSmoke", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var bindingErrors = new List<string>();
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorListener(bindingErrors));

            var app = new PhotoManager.Wpf.App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.InitializeComponent();
            var catalog = new SqlitePhotoCatalog(Path.Combine(folder, "catalog.sqlite3"));
            catalog.InitializeAsync().GetAwaiter().GetResult();
            var scanner = new PhotoScanner(catalog, new FileFingerprintService(), new ImageMetadataReader());
            var vm = new MainWindowViewModel(catalog, scanner, new ThumbnailService(Path.Combine(folder, "thumbs")));
            var window = new MainWindow { DataContext = vm, ShowInTaskbar = false, WindowState = WindowState.Minimized };
            window.Show();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.Close();
            app.Shutdown();

            if (bindingErrors.Count == 0) return 0;
            Console.Error.WriteLine(string.Join(Environment.NewLine, bindingErrors));
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    private sealed class BindingErrorListener(List<string> errors) : TraceListener
    {
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) errors.Add(message); }
        public override void WriteLine(string? message) { if (!string.IsNullOrWhiteSpace(message)) errors.Add(message); }
    }
}
