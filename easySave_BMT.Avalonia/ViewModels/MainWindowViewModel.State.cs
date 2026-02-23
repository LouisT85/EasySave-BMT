using Avalonia.Collections;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel
    {
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
        public bool HasSelection
        {
            get => _hasSelection;
            private set => this.RaiseAndSetIfChanged(ref _hasSelection, value);
        }

        private string _newSaveName = string.Empty;
        public string NewSaveName { get => _newSaveName; set => this.RaiseAndSetIfChanged(ref _newSaveName, value); }

        private string _newSaveSourcePath = string.Empty;
        public string NewSaveSourcePath { get => _newSaveSourcePath; set => this.RaiseAndSetIfChanged(ref _newSaveSourcePath, value); }

        private string _newSaveDestinationPath = string.Empty;
        public string NewSaveDestinationPath { get => _newSaveDestinationPath; set => this.RaiseAndSetIfChanged(ref _newSaveDestinationPath, value); }

        private BackupTypeItem? _selectedBackupTypeItem;
        public BackupTypeItem? SelectedBackupTypeItem
        {
            get => _selectedBackupTypeItem;
            set => this.RaiseAndSetIfChanged(ref _selectedBackupTypeItem, value);
        }

        // --- Config ---
        private string _configLogDirectory = string.Empty;
        public string ConfigLogDirectory { get => _configLogDirectory; set => this.RaiseAndSetIfChanged(ref _configLogDirectory, value); }

        private string _configStateFilePath = string.Empty;
        public string ConfigStateFilePath { get => _configStateFilePath; set => this.RaiseAndSetIfChanged(ref _configStateFilePath, value); }

        private string _configLanguage = "fr";
        public string ConfigLanguage
        {
            get => _configLanguage;
            set => this.RaiseAndSetIfChanged(ref _configLanguage, value);
        }

        private string _configLanguageDraft = "fr";
        public string ConfigLanguageDraft { get => _configLanguageDraft; set => this.RaiseAndSetIfChanged(ref _configLanguageDraft, value); }

        private bool _configEnableEncryptionDraft;
        public bool ConfigEnableEncryptionDraft
        {
            get => _configEnableEncryptionDraft;
            set => this.RaiseAndSetIfChanged(ref _configEnableEncryptionDraft, value);
        }

        private string _configBusinessSoftwareDraft = "";
        public string ConfigBusinessSoftwareDraft
        {
            get => _configBusinessSoftwareDraft;
            set => this.RaiseAndSetIfChanged(ref _configBusinessSoftwareDraft, value);
        }

        public ObservableCollection<string> ConfigBusinessSoftwareEntriesDraft { get; } = new();

        private string _newBusinessSoftwareEntry = "";
        public string NewBusinessSoftwareEntry
        {
            get => _newBusinessSoftwareEntry;
            set => this.RaiseAndSetIfChanged(ref _newBusinessSoftwareEntry, value);
        }

        private string? _selectedBusinessSoftwareEntry;
        public string? SelectedBusinessSoftwareEntry
        {
            get => _selectedBusinessSoftwareEntry;
            set => this.RaiseAndSetIfChanged(ref _selectedBusinessSoftwareEntry, value);
        }

        public ObservableCollection<string> ConfigEncryptionExtensionsDraft { get; } = new();

        private string _newEncryptionExtension = "";
        public string NewEncryptionExtension { get => _newEncryptionExtension; set => this.RaiseAndSetIfChanged(ref _newEncryptionExtension, value); }

        private string? _selectedEncryptionExtension;
        public string? SelectedEncryptionExtension
        {
            get => _selectedEncryptionExtension;
            set => this.RaiseAndSetIfChanged(ref _selectedEncryptionExtension, value);
        }

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

        private bool _isBackupRunning;
        public bool IsBackupRunning
        {
            get => _isBackupRunning;
            private set
            {
                this.RaiseAndSetIfChanged(ref _isBackupRunning, value);
                this.RaisePropertyChanged(nameof(PauseButtonText));
                this.RaisePropertyChanged(nameof(PauseButtonSymbol));
            }
        }

        private bool _isBackupPaused;
        public bool IsBackupPaused
        {
            get => _isBackupPaused;
            private set
            {
                this.RaiseAndSetIfChanged(ref _isBackupPaused, value);
                this.RaisePropertyChanged(nameof(PauseButtonText));
                this.RaisePropertyChanged(nameof(PauseButtonSymbol));
            }
        }

        public string PauseButtonText => IsBackupPaused ? Loc["UiResume"] : Loc["UiPause"];
        public string PauseButtonSymbol => IsBackupPaused ? "▶" : "⏸";
        public string StopButtonSymbol => "⏹";

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

        private void UpdateHasSelection()
        {
            HasSelection = (SelectedSaves.Count > 0) || (SelectedSave != null);
        }

        private void SelectedSaves_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateHasSelection();
        }
    }
}
