using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel
    {
        // --- Commands ---
        public ReactiveCommand<Unit, Unit> ListCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> SelectAllCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> LaunchCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> BrowseSourceCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> BrowseDestinationCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> BrowseLogDirectoryCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> BrowseStateFilePathCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> ConfigCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> AddEncryptionExtensionCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> RemoveEncryptionExtensionCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> LoadLogsCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> StopCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> QuitCommand { get; private set; } = null!;

        private void InitCommands()
        {
            ListCommand = ReactiveCommand.Create(() =>
            {
                // Refresh should also clear any current selection (requested behavior).
                SelectedSaves.Clear();
                SelectedSave = null;
                ListSaves(showUserFeedback: true);
            });

            SelectAllCommand = ReactiveCommand.Create(() =>
            {
                SelectedSaves.Clear();
                foreach (var save in Saves)
                {
                    SelectedSaves.Add(save);
                }
                SelectedSave = Saves.FirstOrDefault();
            });

            var canAdd = this.WhenAnyValue(
                x => x.NewSaveName,
                x => x.NewSaveSourcePath,
                x => x.NewSaveDestinationPath,
                (name, src, dst) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    !string.IsNullOrWhiteSpace(src) &&
                    !string.IsNullOrWhiteSpace(dst));

            AddCommand = ReactiveCommand.Create(AddSave, canAdd);

            SelectedSaves.CollectionChanged += SelectedSaves_CollectionChanged;

            RemoveCommand = ReactiveCommand.Create(RemoveSave);
            LaunchCommand = ReactiveCommand.CreateFromTask(LaunchBackupAsync);

            BrowseSourceCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await BrowseFolderInteraction.Handle(new FolderPickerOpenOptions
                {
                    Title = "Choisir le dossier source",
                    AllowMultiple = false
                });

                if (!string.IsNullOrWhiteSpace(path))
                    NewSaveSourcePath = path;
            });

            BrowseDestinationCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await BrowseFolderInteraction.Handle(new FolderPickerOpenOptions
                {
                    Title = "Choisir le dossier destination",
                    AllowMultiple = false
                });

                if (!string.IsNullOrWhiteSpace(path))
                    NewSaveDestinationPath = path;
            });

            BrowseLogDirectoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await BrowseFolderInteraction.Handle(new FolderPickerOpenOptions
                {
                    Title = "Choisir le répertoire des logs",
                    AllowMultiple = false
                });

                if (!string.IsNullOrWhiteSpace(path))
                    ConfigLogDirectory = path;
            });

            BrowseStateFilePathCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await SaveFileInteraction.Handle(new FilePickerSaveOptions
                {
                    Title = "Choisir le fichier d'état (state.json)",
                    SuggestedFileName = "state.json",
                    DefaultExtension = "json"
                });

                if (!string.IsNullOrWhiteSpace(path))
                    ConfigStateFilePath = path;
            });

            LoadConfigCommand = ReactiveCommand.Create(LoadConfigValuesFromModel);
            ConfigCommand = ReactiveCommand.Create(SaveConfigFromViewModel);

            AddEncryptionExtensionCommand = ReactiveCommand.Create(AddEncryptionExtension);
            RemoveEncryptionExtensionCommand = ReactiveCommand.Create(RemoveEncryptionExtension);

            LoadLogsCommand = ReactiveCommand.Create(LoadLogs);

            StopCommand = ReactiveCommand.Create(() =>
            {
                _coreViewModel.model.RequestStop(easySave_BMT.Model_.BackupStopReason.UserRequested);
                DashboardStatusText = Loc["UiStopRequested"];
            });

            QuitCommand = ReactiveCommand.Create(ShutdownApp);
        }

        private void AddEncryptionExtension()
        {
            string ext = (NewEncryptionExtension ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ext))
            {
                SetTimedAreaMessage(MessageArea.Config, Loc["UiEnterExtension"]);
                return;
            }

            if (!ext.StartsWith(".")) ext = "." + ext;
            ext = ext.ToLowerInvariant();

            // Basic validation
            if (ext.Length < 2 ||
                ext.Any(ch => char.IsWhiteSpace(ch)) ||
                ext.Contains(System.IO.Path.DirectorySeparatorChar) ||
                ext.Contains(System.IO.Path.AltDirectorySeparatorChar))
            {
                SetTimedAreaMessage(MessageArea.Config, Loc["UiInvalidExtension"]);
                return;
            }

            if (!ConfigEncryptionExtensionsDraft.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigEncryptionExtensionsDraft.Add(ext);
            }

            NewEncryptionExtension = "";
        }

        private void RemoveEncryptionExtension()
        {
            if (string.IsNullOrWhiteSpace(SelectedEncryptionExtension)) return;
            ConfigEncryptionExtensionsDraft.Remove(SelectedEncryptionExtension);
            SelectedEncryptionExtension = null;
        }
    }
}

