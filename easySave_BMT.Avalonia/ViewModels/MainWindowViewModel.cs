using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Threading; // Important pour les mises à jour UI depuis un thread
using easySave_BMT.Avalonia.Services;
using easySave_BMT.Model_;
using easySave_BMT.ViewModel_; // Votre ViewModel Core
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace easySave_BMT.Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IProgressObserverGUI
    {
        private readonly ViewModel _coreViewModel;
        public Window? HostWindow { get; set; }

        private readonly Dictionary<string, int> _uiProgressBySaveName = new(StringComparer.Ordinal);

        private enum MessageArea
        {
            Dashboard,
            NewTask,
            Config
        }

        // --- Collections ---
        public ObservableCollection<Model_.Save> Saves { get; } = new();
        public ObservableCollection<BackupTypeItem> BackupTypeOptions { get; } = new();
        public ObservableCollection<string> LanguageOptions { get; } = new() { "fr", "en" };
        public ObservableCollection<string> LogFiles { get; } = new();

        // --- Sélection & Inputs ---
        private Model_.Save? _selectedSave;
        public Model_.Save? SelectedSave
        {
            get => _selectedSave;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSave, value);
                UpdateHasSelection();
            }
        }

        private AvaloniaList<object> _selectedSaves = new();
        public AvaloniaList<object> SelectedSaves
        {
            get => _selectedSaves;
            set
            {
                if (ReferenceEquals(_selectedSaves, value)) return;

                _selectedSaves.CollectionChanged -= SelectedSaves_CollectionChanged;
                _selectedSaves = value ?? new AvaloniaList<object>();
                _selectedSaves.CollectionChanged += SelectedSaves_CollectionChanged;
                this.RaisePropertyChanged(nameof(SelectedSaves));
                UpdateHasSelection();
            }
        }

        private bool _hasSelection;
        public bool HasSelection { get => _hasSelection; private set => this.RaiseAndSetIfChanged(ref _hasSelection, value); }

        private string _newSaveName = string.Empty;
        public string NewSaveName { get => _newSaveName; set => this.RaiseAndSetIfChanged(ref _newSaveName, value); }

        private string _newSaveSourcePath = string.Empty;
        public string NewSaveSourcePath { get => _newSaveSourcePath; set => this.RaiseAndSetIfChanged(ref _newSaveSourcePath, value); }

        private string _newSaveDestinationPath = string.Empty;
        public string NewSaveDestinationPath { get => _newSaveDestinationPath; set => this.RaiseAndSetIfChanged(ref _newSaveDestinationPath, value); }

        private BackupTypeItem? _selectedBackupTypeItem;
        public BackupTypeItem? SelectedBackupTypeItem { get => _selectedBackupTypeItem; set => this.RaiseAndSetIfChanged(ref _selectedBackupTypeItem, value); }

        // --- Config ---
        private string _configLogDirectory = string.Empty;
        public string ConfigLogDirectory { get => _configLogDirectory; set => this.RaiseAndSetIfChanged(ref _configLogDirectory, value); }

        private string _configStateFilePath = string.Empty;
        public string ConfigStateFilePath { get => _configStateFilePath; set => this.RaiseAndSetIfChanged(ref _configStateFilePath, value); }

        private string _configLanguage = "fr";
        public string ConfigLanguage
        {
            get => _configLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref _configLanguage, value);
            }
        }

        private string _configLanguageDraft = "fr";
        public string ConfigLanguageDraft { get => _configLanguageDraft; set => this.RaiseAndSetIfChanged(ref _configLanguageDraft, value); }

        public LocalizationService Loc { get; } = new();

        // --- Logs ---
        private string _selectedLogFile = string.Empty;
        public string SelectedLogFile
        {
            get => _selectedLogFile;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogFile, value);
                ViewSelectedLog(); // Auto-load content on selection
            }
        }

        private string _selectedLogContent = "";
        public string SelectedLogContent { get => _selectedLogContent; set => this.RaiseAndSetIfChanged(ref _selectedLogContent, value); }

        // --- Status & Progress ---
        private int _progressPercent = 0;
        public int ProgressPercent { get => _progressPercent; set => this.RaiseAndSetIfChanged(ref _progressPercent, value); }

        private bool _isProgressVisible = false;
        public bool IsProgressVisible { get => _isProgressVisible; set => this.RaiseAndSetIfChanged(ref _isProgressVisible, value); }

        private string _progressText = "";
        public string ProgressText { get => _progressText; set => this.RaiseAndSetIfChanged(ref _progressText, value); }

        private int _selectedTabIndex;
        public int SelectedTabIndex { get => _selectedTabIndex; set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value); }

        private string _dashboardMessage = string.Empty;
        public string DashboardMessage { get => _dashboardMessage; set => this.RaiseAndSetIfChanged(ref _dashboardMessage, value); }

        private string _dashboardStatusText = string.Empty;
        public string DashboardStatusText { get => _dashboardStatusText; set => this.RaiseAndSetIfChanged(ref _dashboardStatusText, value); }

        private string _newTaskMessage = string.Empty;
        public string NewTaskMessage
        {
            get => _newTaskMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _newTaskMessage, value);
                this.RaisePropertyChanged(nameof(IsNewTaskBannerVisible));
            }
        }

        private string _newTaskStatusText = string.Empty;
        public string NewTaskStatusText
        {
            get => _newTaskStatusText;
            set
            {
                this.RaiseAndSetIfChanged(ref _newTaskStatusText, value);
                this.RaisePropertyChanged(nameof(IsNewTaskBannerVisible));
            }
        }

        private string _configMessage = string.Empty;
        public string ConfigMessage
        {
            get => _configMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _configMessage, value);
                this.RaisePropertyChanged(nameof(IsConfigBannerVisible));
            }
        }

        public bool IsNewTaskBannerVisible =>
            !string.IsNullOrWhiteSpace(NewTaskMessage) ||
            !string.IsNullOrWhiteSpace(NewTaskStatusText);

        public bool IsConfigBannerVisible =>
            !string.IsNullOrWhiteSpace(ConfigMessage);

        // --- Commands ---
        public ReactiveCommand<Unit, Unit> ListCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
        public ReactiveCommand<Unit, Unit> LaunchCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseSourceCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseDestinationCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseLogDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseStateFilePathCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadLogsCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitCommand { get; }

        public Interaction<FolderPickerOpenOptions, string?> BrowseFolderInteraction { get; } = new();
        public Interaction<FilePickerSaveOptions, string?> SaveFileInteraction { get; } = new();

        public MainWindowViewModel()
        {
            // Initialisation du Core
            _coreViewModel = new ViewModel();
            _coreViewModel.guiView = this;
            _coreViewModel.RunAppGUI(this);

            // Commandes
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
                if (!string.IsNullOrWhiteSpace(path)) NewSaveSourcePath = path;
            });

            BrowseDestinationCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await BrowseFolderInteraction.Handle(new FolderPickerOpenOptions
                {
                    Title = "Choisir le dossier destination",
                    AllowMultiple = false
                });
                if (!string.IsNullOrWhiteSpace(path)) NewSaveDestinationPath = path;
            });

            BrowseLogDirectoryCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await BrowseFolderInteraction.Handle(new FolderPickerOpenOptions
                {
                    Title = "Choisir le répertoire des logs",
                    AllowMultiple = false
                });
                if (!string.IsNullOrWhiteSpace(path)) ConfigLogDirectory = path;
            });

            BrowseStateFilePathCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var path = await SaveFileInteraction.Handle(new FilePickerSaveOptions
                {
                    Title = "Choisir le fichier d'état (state.json)",
                    SuggestedFileName = "state.json",
                    DefaultExtension = "json"
                });
                if (!string.IsNullOrWhiteSpace(path)) ConfigStateFilePath = path;
            });

            LoadConfigCommand = ReactiveCommand.Create(LoadConfigValuesFromModel);
            ConfigCommand = ReactiveCommand.Create(SaveConfigFromViewModel);
            LoadLogsCommand = ReactiveCommand.Create(LoadLogs);
            QuitCommand = ReactiveCommand.Create(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            });

            // Chargement initial
            LoadConfigValuesFromModel();
            LoadLogs();
            RefreshBackupTypeOptions();
            ListSaves(showUserFeedback: false);

            SetMessageFromCode(100, MessageArea.Dashboard);
            SelectedLogContent = Loc["UiSelectLogFile"];
        }

        // --- Logique Métier ---

        private static string GetMessageKeyFromCode(int code)
        {
            // Map the existing console codes to localized strings.
            // Use keys from Resources/Strings(.fr).resx.
            return code switch
            {
                100 => "UiReady",
                101 => "FileAddedSuccess",
                103 => "FileDeletedSuccess",
                104 => "BackupSuccess",
                105 => "NoChanges",
                200 => "RestoreJSON",
                201 => "AddFailed",
                202 => "SaveFailed",
                203 => "DeleteFailed",
                204 => "ListEmpty",
                205 => "ListFull",
                206 => "InvalidOption",
                207 => "TransferFailed",
                208 => "BackupTypeNotExist",
                209 => "CopyFailed",
                210 => "CreateFolderFailed",
                211 => "DirectoryNotExist",
                212 => "ChooseDifferentPath",
                213 => "DestinationNotExist",
                214 => "NameTaken",
                215 => "EnterValidName",
                216 => "BackupCompletedWithErrors",
                217 => "DestinationInsideSource",
                218 => "ConfigUpdated",
                _ => "UnknownError"
            };
        }

        private void SetAreaMessage(MessageArea area, string message)
        {
            switch (area)
            {
                case MessageArea.NewTask:
                    NewTaskMessage = message;
                    break;
                case MessageArea.Config:
                    ConfigMessage = message;
                    break;
                default:
                    DashboardMessage = message;
                    break;
            }
        }

        private void SetMessageFromCode(int code, MessageArea area)
        {
            string key = GetMessageKeyFromCode(code);
            SetAreaMessage(area, Loc[key]);
        }

        private void ListSaves(bool showUserFeedback)
        {
            var selectedNames = SelectedSaves.OfType<Model_.Save>().Select(s => s.name).Distinct().ToList();
            if (SelectedSave is not null && !selectedNames.Contains(SelectedSave.name))
            {
                selectedNames.Add(SelectedSave.name);
            }

            int reloadResult = _coreViewModel.saveListManager.DisplaySaves();
            ApplyUiProgressCacheToSaves();
            if (showUserFeedback)
            {
                if (reloadResult == 100)
                {
                    if (_coreViewModel.model.saves.Count > 0)
                        DashboardStatusText = string.Format(Loc["UiListUpdated"], _coreViewModel.model.saves.Count);
                    else
                        DashboardStatusText = Loc["UiNoBackupsDefined"];
                }
                // On error, SaveListManager already pushed a message via guiView.ShowMessage().
            }

            if (selectedNames.Count == 0)
            {
                SelectedSaves.Clear();
                SelectedSave = null;
                return;
            }

            // Re-select items after refresh (ReloadSavesFromFile replaces instances).
            SelectedSaves.Clear();
            foreach (var name in selectedNames)
            {
                var match = Saves.FirstOrDefault(s => string.Equals(s.name, name, StringComparison.Ordinal));
                if (match is not null) SelectedSaves.Add(match);
            }

            SelectedSave = SelectedSaves.OfType<Model_.Save>().FirstOrDefault()
                ?? Saves.FirstOrDefault(s => string.Equals(s.name, selectedNames[0], StringComparison.Ordinal));
        }

        private void AddSave()
        {
            if (string.IsNullOrWhiteSpace(NewSaveName) || string.IsNullOrWhiteSpace(NewSaveSourcePath) || string.IsNullOrWhiteSpace(NewSaveDestinationPath))
            {
                NewTaskMessage = Loc["UiFillAllFields"];
                NewTaskStatusText = "";
                return;
            }

            BackupType type = SelectedBackupTypeItem?.Type ?? BackupType.FULL;
            int res = _coreViewModel.model.AddSave(NewSaveName, NewSaveSourcePath, NewSaveDestinationPath, type);

            SetMessageFromCode(res, MessageArea.NewTask);
            NewTaskStatusText = "";

            if (res == 101)
            {
                SelectedSaves.Clear();
                SelectedSave = null;
                ListSaves(showUserFeedback: false); // Refresh UI
                // Reset champs uniquement en cas de succes
                NewSaveName = ""; NewSaveSourcePath = ""; NewSaveDestinationPath = "";
            }
        }

        private void RemoveSave()
        {
            var names = GetSelectedSaveNames();
            if (names.Count == 0)
            {
                DashboardMessage = Loc["UiSelectBackup"];
                DashboardStatusText = "";
                return;
            }

            // Remove by descending index to avoid shifting.
            var indices = _coreViewModel.model.saves
                .Select((s, idx) => new { s, idx })
                .Where(x => names.Contains(x.s.name))
                .Select(x => x.idx)
                .Where(i => i >= 0)
                .OrderByDescending(i => i)
                .ToList();

            foreach (var idx in indices)
            {
                _coreViewModel.model.RemoveSave(idx);
            }

            SelectedSaves.Clear();
            SelectedSave = null;
            ListSaves(showUserFeedback: false);
            SetMessageFromCode(indices.Count > 0 ? 103 : 203, MessageArea.Dashboard);
            DashboardStatusText = "";
        }

        private async Task LaunchBackupAsync()
        {
            var names = GetSelectedSaveNames();
            if (names.Count == 0)
            {
                DashboardMessage = Loc["UiSelectBackup"];
                DashboardStatusText = "";
                return;
            }

            ProgressPercent = 0;
            IsProgressVisible = true;

            int lastResult = 0;
            try
            {
                // Reload from file so a task still runs correctly after closing/reopening the app.
                ListSaves(showUserFeedback: false);

                var toRun = _coreViewModel.model.saves.Where(s => names.Contains(s.name)).ToList();
                if (toRun.Count == 0)
                {
                    DashboardMessage = Loc["UiSelectBackup"];
                    DashboardStatusText = "";
                    return;
                }

                foreach (var save in toRun)
                {
                    // Reset per-save progress at the start of each run.
                    SetSaveUiProgress(save.name, 0);

                    DashboardMessage = string.Format(Loc["UiLaunchingBackup"], save.name);
                    DashboardStatusText = "";
                    lastResult = await Task.Run(() => _coreViewModel.backupLauncher.LaunchBackupType(save));

                    if (lastResult == 104 || lastResult == 105 || lastResult == 216)
                    {
                        _coreViewModel.model.FinishBackup(save);

                        // Ensure the item shows completed progress even if the strategy had no files to copy.
                        SetSaveUiProgress(save.name, 100);
                    }
                }
            }
            catch (Exception ex)
            {
                DashboardMessage = string.Format(Loc["UiBackupException"], ex.Message);
                DashboardStatusText = "";
                return;
            }

            // Update last backup dates before showing the final result message.
            ListSaves(showUserFeedback: false);

            SetMessageFromCode(lastResult, MessageArea.Dashboard);
            if (names.Count > 1)
            {
                DashboardStatusText = Loc["UiBackupsFinished"];
            }
            else if (lastResult == 105)
            {
                // Differential backup with no changes: avoid confusing "finished in 0s" status text.
                DashboardStatusText = "";
            }
        }

        private void LoadConfigValuesFromModel()
        {
            try
            {
                var cfg = _coreViewModel.model.GetConfig();
                ConfigLogDirectory = cfg.LogDirectory;
                ConfigStateFilePath = cfg.StateFilePath;
                ConfigLanguage = cfg.Language;
                ConfigLanguageDraft = cfg.Language;
                Loc.SetLanguage(cfg.Language);
                RefreshBackupTypeOptions();
                ConfigMessage = "";
            }
            catch (Exception ex)
            {
                ConfigMessage = string.Format(Loc["UiConfigLoadError"], ex.Message);
            }
        }

        private void SaveConfigFromViewModel()
        {
            try
            {
                _coreViewModel.model.UpdateConfig(ConfigLogDirectory, ConfigStateFilePath, ConfigLanguageDraft);

                // Apply language to the current UI only after Save.
                ConfigLanguage = ConfigLanguageDraft;
                Loc.SetLanguage(ConfigLanguageDraft);
                RefreshBackupTypeOptions();

                LoadConfigValuesFromModel();
                LoadLogs();
                SelectedLogFile = string.Empty;
                SelectedLogContent = Loc["UiSelectLogFile"];
                SetMessageFromCode(218, MessageArea.Config);
            }
            catch (Exception ex)
            {
                ConfigMessage = string.Format(Loc["UiConfigSaveError"], ex.Message);
            }
        }

        private void LoadLogs()
        {
            try
            {
                LogFiles.Clear();
                if (Directory.Exists(ConfigLogDirectory))
                {
                    var files = Directory.GetFiles(ConfigLogDirectory).OrderByDescending(f => f);
                    foreach (var f in files) LogFiles.Add(Path.GetFileName(f));
                }
                if (LogFiles.Count == 0)
                {
                    SelectedLogContent = Loc["UiNoLogsFound"];
                }
            }
            catch (Exception ex)
            {
                SelectedLogContent = string.Format(Loc["UiLogsLoadError"], ex.Message);
            }
        }

        private void ViewSelectedLog()
        {
            if (string.IsNullOrEmpty(SelectedLogFile))
            {
                SelectedLogContent = Loc["UiSelectLogFile"];
                return;
            }
            string path = Path.Combine(ConfigLogDirectory, SelectedLogFile);
            if (File.Exists(path)) SelectedLogContent = File.ReadAllText(path);
        }

        private void SetSaveUiProgress(string backupName, int percent)
        {
            if (string.IsNullOrWhiteSpace(backupName)) return;

            percent = Math.Clamp(percent, 0, 100);
            _uiProgressBySaveName[backupName] = percent;

            var match = Saves.FirstOrDefault(s => string.Equals(s.name, backupName, StringComparison.Ordinal));
            if (match is not null)
            {
                match.UiProgressPercent = percent;
            }
        }

        private void ApplyUiProgressCacheToSaves()
        {
            foreach (var save in Saves)
            {
                if (save is null || string.IsNullOrWhiteSpace(save.name)) continue;

                if (_uiProgressBySaveName.TryGetValue(save.name, out int percent))
                    save.UiProgressPercent = percent;
                else
                    save.UiProgressPercent = 0;
            }
        }

        // --- IProgressObserverGUI Implementation ---
        // Utilisation de Dispatcher.UIThread pour garantir que l'UI se met à jour depuis le thread de backup
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Clamp percent
                ProgressPercent = Math.Clamp(percent, 0, 100);
                ProgressText = $"{backupName}: {percent}% ({Loc["FilesRemaining"]}: {filesLeft})";
                IsProgressVisible = true;
                SetSaveUiProgress(backupName, percent);
            });
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Make sure a differential backup with no changes still shows 100% on the item.
                SetSaveUiProgress(backupName, 100);

                ProgressPercent = 100;
                IsProgressVisible = true;
                DashboardStatusText = string.Format(Loc["UiBackupFinished"], backupName, transferTime);
            });
        }

        public void OnFileError(string fileName)
        {
            Dispatcher.UIThread.Post(() => DashboardStatusText = $"{Loc["CopyFailed"]}: {fileName}");
        }

        public void ShowMessage(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Route generic core messages to the currently visible tab.
                // (Backup actions set DashboardMessage explicitly.)
                if (SelectedTabIndex == 1)
                    NewTaskMessage = message;
                else if (SelectedTabIndex == 3)
                    ConfigMessage = message;
                else
                    DashboardMessage = message;
            });
        }

        private void UpdateHasSelection()
        {
            HasSelection = (SelectedSaves.Count > 0) || (SelectedSave != null);
        }

        private void SelectedSaves_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateHasSelection();
        }

        private List<string> GetSelectedSaveNames()
        {
            var names = SelectedSaves.OfType<Model_.Save>().Select(s => s.name).Distinct().ToList();
            if (SelectedSave is not null && !names.Contains(SelectedSave.name))
            {
                names.Add(SelectedSave.name);
            }
            return names;
        }

        private void RefreshBackupTypeOptions()
        {
            var currentType = SelectedBackupTypeItem?.Type;
            BackupTypeOptions.Clear();
            BackupTypeOptions.Add(new BackupTypeItem(BackupType.FULL, Loc["FullBackup"]));
            BackupTypeOptions.Add(new BackupTypeItem(BackupType.DIFFERENTIAL, Loc["DifferentialBackup"]));

            SelectedBackupTypeItem = BackupTypeOptions.FirstOrDefault(i => i.Type == currentType)
                ?? BackupTypeOptions.FirstOrDefault(i => i.Type == BackupType.FULL);
        }

        public sealed class BackupTypeItem
        {
            public BackupTypeItem(BackupType type, string display)
            {
                Type = type;
                Display = display;
            }

            public BackupType Type { get; }
            public string Display { get; }
        }
    }
}
