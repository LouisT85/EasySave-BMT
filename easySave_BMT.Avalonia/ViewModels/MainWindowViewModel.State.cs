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
        public ObservableCollection<ThemeOptionItem> ThemeOptions { get; } = new();
        public ObservableCollection<string> LogFiles { get; } = new();
        public ObservableCollection<LogSortOptionItem> LogSortOptions { get; } = new();
        public ObservableCollection<LogSortOptionItem> LogEntrySortOptions { get; } = new();

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

        public ObservableCollection<string> LogDestinationModeOptions { get; } = new()
        {
            Model_.Config.LogDestinationModeLocalOnly,
            Model_.Config.LogDestinationModeCentralizedOnly,
            Model_.Config.LogDestinationModeLocalAndCentralized
        };

        private string _configLogDestinationModeDraft = Model_.Config.LogDestinationModeLocalOnly;
        public string ConfigLogDestinationModeDraft
        {
            get => _configLogDestinationModeDraft;
            set
            {
                string normalized = Model_.Config.NormalizeLogDestinationMode(value);
                this.RaiseAndSetIfChanged(ref _configLogDestinationModeDraft, normalized);
                this.RaisePropertyChanged(nameof(IsCentralizedLoggingModeSelected));
            }
        }

        private string _configCentralizedLogEndpoint = string.Empty;
        public string ConfigCentralizedLogEndpoint
        {
            get => _configCentralizedLogEndpoint;
            set => this.RaiseAndSetIfChanged(ref _configCentralizedLogEndpoint, value);
        }

        public bool IsCentralizedLoggingModeSelected =>
            Model_.Config.RequiresCentralizedEndpoint(ConfigLogDestinationModeDraft);

        private string _configTheme = "auto";
        public string ConfigTheme { get => _configTheme; set => this.RaiseAndSetIfChanged(ref _configTheme, value); }

        private string _configThemeDraft = "auto";
        public string ConfigThemeDraft
        {
            get => _configThemeDraft;
            set
            {
                this.RaiseAndSetIfChanged(ref _configThemeDraft, value);
                ApplyThemePreference(value);
            }
        }

        private ThemeOptionItem? _selectedThemeOption;
        public ThemeOptionItem? SelectedThemeOption
        {
            get => _selectedThemeOption;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedThemeOption, value);
                if (value is not null)
                {
                    ConfigThemeDraft = value.Key;
                }
            }
        }
        private bool _configEnableEncryptionDraft;
        public bool ConfigEnableEncryptionDraft
        {
            get => _configEnableEncryptionDraft;
            set => this.RaiseAndSetIfChanged(ref _configEnableEncryptionDraft, value);
        }

        private string _configCryptoSoftKeyDraft = "";
        public string ConfigCryptoSoftKeyDraft
        {
            get => _configCryptoSoftKeyDraft;
            set => this.RaiseAndSetIfChanged(ref _configCryptoSoftKeyDraft, value);
        }

        private string _configBusinessSoftwareDraft = "";
        public string ConfigBusinessSoftwareDraft
        {
            get => _configBusinessSoftwareDraft;
            set => this.RaiseAndSetIfChanged(ref _configBusinessSoftwareDraft, value);
        }

        public ObservableCollection<string> ConfigBusinessSoftwareEntriesDraft { get; } = new();
        public ObservableCollection<string> BusinessSoftwareSuggestions { get; } = new();
        public ObservableCollection<string> EncryptionKeyCreationTraceDraft { get; } = new();
        public ObservableCollection<string> EncryptionExtensionSuggestions { get; } = new();
        public ObservableCollection<string> PriorityExtensionSuggestions { get; } = new();

        private bool _hasBusinessSoftwareSuggestions;
        public bool HasBusinessSoftwareSuggestions
        {
            get => _hasBusinessSoftwareSuggestions;
            set => this.RaiseAndSetIfChanged(ref _hasBusinessSoftwareSuggestions, value);
        }

        private bool _hasEncryptionExtensionSuggestions;
        public bool HasEncryptionExtensionSuggestions
        {
            get => _hasEncryptionExtensionSuggestions;
            set => this.RaiseAndSetIfChanged(ref _hasEncryptionExtensionSuggestions, value);
        }

        private bool _hasPriorityExtensionSuggestions;
        public bool HasPriorityExtensionSuggestions
        {
            get => _hasPriorityExtensionSuggestions;
            set => this.RaiseAndSetIfChanged(ref _hasPriorityExtensionSuggestions, value);
        }

        private string _newBusinessSoftwareEntry = "";
        public string NewBusinessSoftwareEntry
        {
            get => _newBusinessSoftwareEntry;
            set
            {
                this.RaiseAndSetIfChanged(ref _newBusinessSoftwareEntry, value);
                UpdateBusinessSoftwareSuggestions();
            }
        }

        private string? _selectedBusinessSoftwareEntry;
        public string? SelectedBusinessSoftwareEntry
        {
            get => _selectedBusinessSoftwareEntry;
            set => this.RaiseAndSetIfChanged(ref _selectedBusinessSoftwareEntry, value);
        }

        public ObservableCollection<string> ConfigEncryptionExtensionsDraft { get; } = new();
        public ObservableCollection<string> ConfigPriorityExtensionsDraft { get; } = new();

        private string _newEncryptionExtension = "";
        public string NewEncryptionExtension
        {
            get => _newEncryptionExtension;
            set
            {
                this.RaiseAndSetIfChanged(ref _newEncryptionExtension, value);
                UpdateEncryptionExtensionSuggestions();
            }
        }

        private string? _selectedEncryptionExtension;
        public string? SelectedEncryptionExtension
        {
            get => _selectedEncryptionExtension;
            set => this.RaiseAndSetIfChanged(ref _selectedEncryptionExtension, value);
        }

        private string _newPriorityExtension = "";
        public string NewPriorityExtension
        {
            get => _newPriorityExtension;
            set
            {
                this.RaiseAndSetIfChanged(ref _newPriorityExtension, value);
                UpdatePriorityExtensionSuggestions();
            }
        }

        private string? _selectedPriorityExtension;
        public string? SelectedPriorityExtension
        {
            get => _selectedPriorityExtension;
            set => this.RaiseAndSetIfChanged(ref _selectedPriorityExtension, value);
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

        private string _logFilesSummaryText = string.Empty;
        public string LogFilesSummaryText
        {
            get => _logFilesSummaryText;
            set => this.RaiseAndSetIfChanged(ref _logFilesSummaryText, value);
        }

        private string _logFileSearchText = string.Empty;
        public string LogFileSearchText
        {
            get => _logFileSearchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _logFileSearchText, value);
                ApplyLogFileFilterAndSort();
            }
        }

        private LogSortOptionItem? _selectedLogSortOption;
        public LogSortOptionItem? SelectedLogSortOption
        {
            get => _selectedLogSortOption;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogSortOption, value);
                ApplyLogFileFilterAndSort();
            }
        }

        public ObservableCollection<LogEntryViewItem> ParsedLogEntries { get; } = new();

        private LogEntryViewItem? _selectedParsedLogEntry;
        public LogEntryViewItem? SelectedParsedLogEntry
        {
            get => _selectedParsedLogEntry;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedParsedLogEntry, value);
                UpdateSelectedLogEntryDetails();
            }
        }

        private string _logEntrySearchText = string.Empty;
        public string LogEntrySearchText
        {
            get => _logEntrySearchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _logEntrySearchText, value);
                ApplyParsedLogEntryFilterAndSort();
            }
        }

        private LogSortOptionItem? _selectedLogEntrySortOption;
        public LogSortOptionItem? SelectedLogEntrySortOption
        {
            get => _selectedLogEntrySortOption;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogEntrySortOption, value);
                ApplyParsedLogEntryFilterAndSort();
            }
        }

        private bool _showOnlyFailedLogEntries;
        public bool ShowOnlyFailedLogEntries
        {
            get => _showOnlyFailedLogEntries;
            set
            {
                this.RaiseAndSetIfChanged(ref _showOnlyFailedLogEntries, value);
                ApplyParsedLogEntryFilterAndSort();
            }
        }

        private string _logEntriesFilterSummary = string.Empty;
        public string LogEntriesFilterSummary
        {
            get => _logEntriesFilterSummary;
            set => this.RaiseAndSetIfChanged(ref _logEntriesFilterSummary, value);
        }

        private string _logSummaryText = string.Empty;
        public string LogSummaryText
        {
            get => _logSummaryText;
            set => this.RaiseAndSetIfChanged(ref _logSummaryText, value);
        }

        private bool _isStructuredLogVisible;
        public bool IsStructuredLogVisible
        {
            get => _isStructuredLogVisible;
            set => this.RaiseAndSetIfChanged(ref _isStructuredLogVisible, value);
        }

        private bool _isRawLogVisible = true;
        public bool IsRawLogVisible
        {
            get => _isRawLogVisible;
            set => this.RaiseAndSetIfChanged(ref _isRawLogVisible, value);
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
        public string PauseButtonSymbol => IsBackupPaused ? "▶" : "❚❚";
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

        public sealed class ThemeOptionItem
        {
            public ThemeOptionItem(string key, string display)
            {
                Key = key;
                Display = display;
            }

            public string Key { get; }
            public string Display { get; }
        }

        public sealed class LogSortOptionItem
        {
            public LogSortOptionItem(string key, string display)
            {
                Key = key;
                Display = display;
            }

            public string Key { get; }
            public string Display { get; }
        }

        public sealed class LogEntryViewItem
        {
            public string Time { get; init; } = "";
            public string BackupName { get; init; } = "";
            public string SourcePath { get; init; } = "";
            public string TargetPath { get; init; } = "";
            public string MachineName { get; init; } = "";
            public string UserName { get; init; } = "";
            public long FileSizeBytes { get; init; }
            public long TransferTimeMs { get; init; }
            public long EncryptionTimeMs { get; init; }
        }
    }
}
