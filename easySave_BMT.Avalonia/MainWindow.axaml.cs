using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using easySave_BMT.Avalonia.ViewModels;

namespace easySave_BMT.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainWindowViewModel vm)
            {
                vm.HostWindow = this;
            }
        }

        private async void OnBrowseSourceClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            var dialog = new OpenFolderDialog
            {
                Title = "Choisir le dossier source"
            };
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                vm.NewSaveSourcePath = result;
            }
        }

        private async void OnBrowseDestinationClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            var dialog = new OpenFolderDialog
            {
                Title = "Choisir le dossier destination"
            };
            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrEmpty(result))
            {
                vm.NewSaveDestinationPath = result;
            }
        }
    }
}
