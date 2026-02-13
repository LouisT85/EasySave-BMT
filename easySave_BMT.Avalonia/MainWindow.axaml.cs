using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage; // Nouvelle API Avalonia Storage (plus moderne)
using easySave_BMT.Avalonia.ViewModels;
using System;

namespace easySave_BMT.Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Utilisation de la nouvelle API StorageProvider d'Avalonia 11+ (plus compatible)
        // Si vous êtes sur une vieille version, gardez OpenFolderDialog
        private async void OnBrowseSourceClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            // Méthode moderne (StorageProvider)
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choisir le dossier source",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                vm.NewSaveSourcePath = folders[0].Path.LocalPath;
            }
        }

        private async void OnBrowseDestinationClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choisir le dossier destination",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                vm.NewSaveDestinationPath = folders[0].Path.LocalPath;
            }
        }
    }
}