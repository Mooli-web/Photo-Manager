using System.Windows;
using System.Windows.Controls;
using PhotoManager.Wpf.ViewModels;

namespace PhotoManager.Wpf;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void PhotoList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            await vm.SetSelectionAsync(PhotoList.SelectedItems.Cast<PhotoItemViewModel>());
    }
}
