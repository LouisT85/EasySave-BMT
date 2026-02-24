using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using easySave_BMT.Model_;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Security.Cryptography;
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
        public ReactiveCommand<Unit, Unit> GenerateEncryptionKeyCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> AddEncryptionExtensionCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> RemoveEncryptionExtensionCommand { get; private set; } = null!;
        public ReactiveCommand<string, Unit> AddEncryptionExtensionSuggestionCommand { get; private set; } = null!;
        public ReactiveCommand<string, Unit> RemoveEncryptionExtensionByValueCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> AddBusinessSoftwareEntryCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> RemoveBusinessSoftwareEntryCommand { get; private set; } = null!;
        public ReactiveCommand<string, Unit> AddBusinessSoftwareSuggestionCommand { get; private set; } = null!;
        public ReactiveCommand<string, Unit> RemoveBusinessSoftwareEntryByValueCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> LoadLogsCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> PauseCommand { get; private set; } = null!;
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
            GenerateEncryptionKeyCommand = ReactiveCommand.Create(GenerateEncryptionKey);

            AddEncryptionExtensionCommand = ReactiveCommand.Create(AddEncryptionExtension);
            RemoveEncryptionExtensionCommand = ReactiveCommand.Create(RemoveEncryptionExtension);
            AddEncryptionExtensionSuggestionCommand = ReactiveCommand.Create<string>(AddEncryptionExtensionSuggestion);
            RemoveEncryptionExtensionByValueCommand = ReactiveCommand.Create<string>(RemoveEncryptionExtensionByValue);
            AddBusinessSoftwareEntryCommand = ReactiveCommand.Create(AddBusinessSoftwareEntry);
            RemoveBusinessSoftwareEntryCommand = ReactiveCommand.Create(RemoveBusinessSoftwareEntry);
            AddBusinessSoftwareSuggestionCommand = ReactiveCommand.Create<string>(AddBusinessSoftwareSuggestion);
            RemoveBusinessSoftwareEntryByValueCommand = ReactiveCommand.Create<string>(RemoveBusinessSoftwareEntryByValue);

            LoadLogsCommand = ReactiveCommand.Create(LoadLogs);

            PauseCommand = ReactiveCommand.Create(() =>
            {
                if (!IsBackupRunning) return;

                if (!IsBackupPaused)
                {
                    _coreViewModel.model.RequestPause();
                    // Persist current state so if the app is closed while paused, we can resume/overwrite in-place.
                    _coreViewModel.model.AddLogInJSONFile();
                    IsBackupPaused = true;
                    DashboardStatusText = Loc["UiPaused"];
                    ProgressText = Loc["UiPaused"];
                }
                else
                {
                    _coreViewModel.model.ClearPauseRequest();
                    IsBackupPaused = false;
                    DashboardStatusText = Loc["UiResumed"];
                }
            });

            StopCommand = ReactiveCommand.Create(() =>
            {
                _coreViewModel.model.RequestStop(BackupStopReason.UserRequested, detail: "cleanup");
                _coreViewModel.model.ClearPauseRequest();
                IsBackupPaused = false;
                DashboardStatusText = Loc["UiStopRequested"];
            });

            QuitCommand = ReactiveCommand.Create(ShutdownApp);
        }

        private void GenerateEncryptionKey()
        {
            byte[] keyBytes = RandomNumberGenerator.GetBytes(32);
            string generatedKey = "0x" + Convert.ToHexString(keyBytes);

            ConfigCryptoSoftKeyDraft = generatedKey;

            string fingerprint = generatedKey.Length >= 8 ? generatedKey[^8..] : generatedKey;
            string traceEntry = string.Format(
                Loc["UiEncryptionKeyTraceEntry"],
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                fingerprint);

            EncryptionKeyCreationTraceDraft.Insert(0, traceEntry);
            while (EncryptionKeyCreationTraceDraft.Count > 100)
            {
                EncryptionKeyCreationTraceDraft.RemoveAt(EncryptionKeyCreationTraceDraft.Count - 1);
            }

            SetTimedAreaMessage(MessageArea.Config, Loc["UiEncryptionKeyGenerated"]);
        }

        private void AddEncryptionExtension()
        {
            AddEncryptionExtensionFromValue(NewEncryptionExtension, showValidationFeedback: true);
        }

        private void AddEncryptionExtensionSuggestion(string suggestion)
        {
            AddEncryptionExtensionFromValue(suggestion, showValidationFeedback: false);
        }

        private void AddEncryptionExtensionFromValue(string? rawValue, bool showValidationFeedback)
        {
            string ext = (rawValue ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ext))
            {
                if (showValidationFeedback)
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
                if (showValidationFeedback)
                    SetTimedAreaMessage(MessageArea.Config, Loc["UiInvalidExtension"]);
                return;
            }

            if (!ConfigEncryptionExtensionsDraft.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigEncryptionExtensionsDraft.Add(ext);
            }

            NewEncryptionExtension = "";
            UpdateEncryptionExtensionSuggestions();
        }

        private void RemoveEncryptionExtension()
        {
            if (string.IsNullOrWhiteSpace(SelectedEncryptionExtension)) return;
            ConfigEncryptionExtensionsDraft.Remove(SelectedEncryptionExtension);
            SelectedEncryptionExtension = null;
            UpdateEncryptionExtensionSuggestions();
        }

        private void RemoveEncryptionExtensionByValue(string value)
        {
            string normalized = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalized)) return;

            var match = ConfigEncryptionExtensionsDraft
                .FirstOrDefault(v => string.Equals(v, normalized, StringComparison.OrdinalIgnoreCase));

            if (match is null) return;

            ConfigEncryptionExtensionsDraft.Remove(match);
            if (string.Equals(SelectedEncryptionExtension, match, StringComparison.OrdinalIgnoreCase))
                SelectedEncryptionExtension = null;

            UpdateEncryptionExtensionSuggestions();
        }

        private void AddBusinessSoftwareEntry()
        {
            AddBusinessSoftwareEntryFromValue(NewBusinessSoftwareEntry, showValidationFeedback: true);
        }

        private void AddBusinessSoftwareSuggestion(string suggestion)
        {
            AddBusinessSoftwareEntryFromValue(suggestion, showValidationFeedback: false);
        }

        private void AddBusinessSoftwareEntryFromValue(string? rawValue, bool showValidationFeedback)
        {
            string entry = NormalizeBusinessSoftwareEntry(rawValue);
            if (string.IsNullOrWhiteSpace(entry))
            {
                if (showValidationFeedback)
                    SetTimedAreaMessage(MessageArea.Config, Loc["UiEnterBusinessSoftwareEntry"]);
                return;
            }

            if (entry.Contains(';') || entry.Contains(',') || entry.Contains('|') || entry.Contains('\n') || entry.Contains('\r'))
            {
                if (showValidationFeedback)
                    SetTimedAreaMessage(MessageArea.Config, Loc["UiInvalidBusinessSoftwareEntry"]);
                return;
            }

            if (!ConfigBusinessSoftwareEntriesDraft.Any(e => string.Equals(e, entry, StringComparison.OrdinalIgnoreCase)))
            {
                ConfigBusinessSoftwareEntriesDraft.Add(entry);
            }

            NewBusinessSoftwareEntry = "";
            UpdateBusinessSoftwareSuggestions();
        }

        private void RemoveBusinessSoftwareEntry()
        {
            if (string.IsNullOrWhiteSpace(SelectedBusinessSoftwareEntry)) return;
            ConfigBusinessSoftwareEntriesDraft.Remove(SelectedBusinessSoftwareEntry);
            SelectedBusinessSoftwareEntry = null;
            UpdateBusinessSoftwareSuggestions();
        }

        private void RemoveBusinessSoftwareEntryByValue(string value)
        {
            string normalized = NormalizeBusinessSoftwareEntry(value);
            if (string.IsNullOrWhiteSpace(normalized)) return;

            var match = ConfigBusinessSoftwareEntriesDraft
                .FirstOrDefault(v => string.Equals(v, normalized, StringComparison.OrdinalIgnoreCase));

            if (match is null) return;

            ConfigBusinessSoftwareEntriesDraft.Remove(match);
            if (string.Equals(SelectedBusinessSoftwareEntry, match, StringComparison.OrdinalIgnoreCase))
                SelectedBusinessSoftwareEntry = null;

            UpdateBusinessSoftwareSuggestions();
        }

        private static string NormalizeBusinessSoftwareEntry(string? raw)
        {
            string entry = (raw ?? "").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(entry)) return "";

            try
            {
                if (entry.Contains(System.IO.Path.DirectorySeparatorChar) || entry.Contains(System.IO.Path.AltDirectorySeparatorChar))
                {
                    entry = System.IO.Path.GetFileName(entry);
                }
            }
            catch { }

            if (entry.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                entry = entry[..^4];

            return entry.Trim();
        }
    }
}
