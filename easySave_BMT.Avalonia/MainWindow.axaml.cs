using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.VisualTree;
using easySave_BMT.Avalonia.ViewModels;
using ReactiveUI;
using System;
using System.Linq;

namespace easySave_BMT.Avalonia
{
    public partial class MainWindow : Window
    {
        private bool _handlersRegistered;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_handlersRegistered) return;
            if (DataContext is not MainWindowViewModel vm) return;

            _handlersRegistered = true;

            vm.BrowseFolderInteraction.RegisterHandler(async interaction =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    interaction.SetOutput(null);
                    return;
                }

                var options = interaction.Input ?? new FolderPickerOpenOptions
                {
                    Title = "Choisir un dossier",
                    AllowMultiple = false
                };

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
                interaction.SetOutput(folders.Count > 0 ? folders[0].Path.LocalPath : null);
            });

            vm.SaveFileInteraction.RegisterHandler(async interaction =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null)
                {
                    interaction.SetOutput(null);
                    return;
                }

                var options = interaction.Input ?? new FilePickerSaveOptions
                {
                    Title = "Enregistrer un fichier",
                    SuggestedFileName = "state.json",
                    DefaultExtension = "json"
                };

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
                interaction.SetOutput(file?.Path.LocalPath);
            });
        }

        private void OnDashboardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed != true) return;
            if (DataContext is not MainWindowViewModel vm) return;

            var visual = e.Source as Visual;
            if (visual != null)
            {
                // Ignore right-clicks on action buttons.
                if (visual.GetVisualAncestors().OfType<Button>().Any()) return;

                // Ignore right-clicks on a save item itself.
                if (visual.GetVisualAncestors().OfType<ListBoxItem>().Any()) return;
            }

            vm.SelectedSaves.Clear();
            vm.SelectedSave = null;
            e.Handled = true;
        }
    }
}
