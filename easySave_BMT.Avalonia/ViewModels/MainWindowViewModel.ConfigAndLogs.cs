using easySave_BMT.Model_;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel
    {
        private const string ThemeAuto = "auto";
        private const string ThemeLight = "light";
        private const string ThemeDark = "dark";

        private const string SortNewest = "newest";
        private const string SortOldest = "oldest";
        private const string SortName = "name";
        private static readonly TimeSpan BusinessSuggestionCacheDuration = TimeSpan.FromSeconds(5);

        private static readonly string[] BuiltInBusinessSoftwareSuggestions =
        {
            "word",
            "winword",
            "excel",
            "powerpnt",
            "outlook",
            "notepad",
            "calc",
            "calculatorapp",
            "chrome",
            "firefox",
            "explorer",
            "teams",
            "devenv",
            "code",
            "cmd",
            "powershell"
        };

        private static readonly string[] BuiltInExtensionSuggestions =
        {
            ".txt",
            ".docx",
            ".doc",
            ".xlsx",
            ".xls",
            ".pptx",
            ".ppt",
            ".pdf",
            ".json",
            ".xml",
            ".csv",
            ".zip",
            ".bak",
            ".log"
        };

        private readonly List<string> _allLogFiles = new();
        private readonly List<LogEntryViewItem> _allParsedLogEntries = new();
        private readonly List<string> _cachedBusinessSuggestionPool = new();
        private DateTime _businessSuggestionCacheTimestampUtc = DateTime.MinValue;

        private System.Collections.Generic.List<string> BuildNormalizedEncryptionExtensionsDraft()
        {
            return ConfigEncryptionExtensionsDraft
                .Select(e => (e ?? "").Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private System.Collections.Generic.List<string> BuildNormalizedPriorityExtensionsDraft()
        {
            return ConfigPriorityExtensionsDraft
                .Select(e => (e ?? "").Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void UpdateBusinessSoftwareSuggestions()
        {
            string query = NormalizeBusinessSoftwareEntry(NewBusinessSoftwareEntry);

            if (string.IsNullOrWhiteSpace(query))
            {
                BusinessSoftwareSuggestions.Clear();
                HasBusinessSoftwareSuggestions = false;
                return;
            }

            var selected = new HashSet<string>(
                ConfigBusinessSoftwareEntriesDraft.Select(NormalizeBusinessSoftwareEntry)
                    .Where(s => !string.IsNullOrWhiteSpace(s)),
                StringComparer.OrdinalIgnoreCase);

            var suggestions = GetBusinessSoftwareSuggestionPool()
                .Where(name => name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Where(name => !selected.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Take(18)
                .ToList();

            BusinessSoftwareSuggestions.Clear();
            foreach (string suggestion in suggestions)
                BusinessSoftwareSuggestions.Add(suggestion);

            HasBusinessSoftwareSuggestions = BusinessSoftwareSuggestions.Count > 0;
        }

        private IEnumerable<string> GetBusinessSoftwareSuggestionPool()
        {
            if (_cachedBusinessSuggestionPool.Count > 0 &&
                DateTime.UtcNow - _businessSuggestionCacheTimestampUtc < BusinessSuggestionCacheDuration)
            {
                return _cachedBusinessSuggestionPool;
            }

            var pool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInBusinessSoftwareSuggestions)
                pool.Add(builtIn);

            foreach (string configured in ConfigBusinessSoftwareEntriesDraft)
            {
                string normalized = NormalizeBusinessSoftwareEntry(configured);
                if (!string.IsNullOrWhiteSpace(normalized))
                    pool.Add(normalized);
            }

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        string processName = (process.ProcessName ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(processName))
                            pool.Add(processName);
                    }
                    catch { }
                    finally
                    {
                        try { process.Dispose(); } catch { }
                    }
                }
            }
            catch { }

            _cachedBusinessSuggestionPool.Clear();
            _cachedBusinessSuggestionPool.AddRange(pool.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            _businessSuggestionCacheTimestampUtc = DateTime.UtcNow;

            return _cachedBusinessSuggestionPool;
        }

        private void UpdateEncryptionExtensionSuggestions()
        {
            string query = NormalizeExtensionSuggestionQuery(NewEncryptionExtension);

            if (string.IsNullOrWhiteSpace(query))
            {
                EncryptionExtensionSuggestions.Clear();
                HasEncryptionExtensionSuggestions = false;
                return;
            }

            var selected = new HashSet<string>(
                ConfigEncryptionExtensionsDraft.Select(NormalizeExtensionSuggestionQuery)
                    .Where(s => !string.IsNullOrWhiteSpace(s)),
                StringComparer.OrdinalIgnoreCase);

            var pool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInExtensionSuggestions)
                pool.Add(builtIn);

            foreach (string configured in ConfigEncryptionExtensionsDraft)
            {
                string normalized = NormalizeExtensionSuggestionQuery(configured);
                if (!string.IsNullOrWhiteSpace(normalized))
                    pool.Add(normalized);
            }

            var suggestions = pool
                .Where(ext => ext.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Where(ext => !selected.Contains(ext))
                .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
                .Take(18)
                .ToList();

            EncryptionExtensionSuggestions.Clear();
            foreach (string suggestion in suggestions)
                EncryptionExtensionSuggestions.Add(suggestion);

            HasEncryptionExtensionSuggestions = EncryptionExtensionSuggestions.Count > 0;
        }

        private void UpdatePriorityExtensionSuggestions()
        {
            string query = NormalizeExtensionSuggestionQuery(NewPriorityExtension);

            if (string.IsNullOrWhiteSpace(query))
            {
                PriorityExtensionSuggestions.Clear();
                HasPriorityExtensionSuggestions = false;
                return;
            }

            var selected = new HashSet<string>(
                ConfigPriorityExtensionsDraft.Select(NormalizeExtensionSuggestionQuery)
                    .Where(s => !string.IsNullOrWhiteSpace(s)),
                StringComparer.OrdinalIgnoreCase);

            var pool = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string builtIn in BuiltInExtensionSuggestions)
                pool.Add(builtIn);

            foreach (string configured in ConfigPriorityExtensionsDraft)
            {
                string normalized = NormalizeExtensionSuggestionQuery(configured);
                if (!string.IsNullOrWhiteSpace(normalized))
                    pool.Add(normalized);
            }

            var suggestions = pool
                .Where(ext => ext.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Where(ext => !selected.Contains(ext))
                .OrderBy(ext => ext, StringComparer.OrdinalIgnoreCase)
                .Take(18)
                .ToList();

            PriorityExtensionSuggestions.Clear();
            foreach (string suggestion in suggestions)
                PriorityExtensionSuggestions.Add(suggestion);

            HasPriorityExtensionSuggestions = PriorityExtensionSuggestions.Count > 0;
        }

        private static string NormalizeExtensionSuggestionQuery(string? raw)
        {
            string value = (raw ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value)) return "";
            if (!value.StartsWith(".")) value = "." + value;
            return value;
        }

        private void ApplyEncryptionDraftToModelForLaunch()
        {
            var cfg = _coreViewModel.model.GetConfig();
            var exts = BuildNormalizedEncryptionExtensionsDraft();
            var priorityExts = BuildNormalizedPriorityExtensionsDraft();
            string cryptoSoftKey = (ConfigCryptoSoftKeyDraft ?? "").Trim();
            var keyTrace = EncryptionKeyCreationTraceDraft
                .Select(e => (e ?? "").Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            _coreViewModel.model.UpdateConfig(
                cfg.LogDirectory,
                cfg.StateFilePath,
                cfg.Language,
                enableEncryption: ConfigEnableEncryptionDraft,
                encryptionExtensions: exts,
                priorityExtensions: priorityExts,
                cryptoSoftKey: cryptoSoftKey,
                encryptionKeyCreationTrace: keyTrace);
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
                ConfigLogDestinationModeDraft = Config.NormalizeLogDestinationMode(cfg.LogDestinationMode);
                ConfigCentralizedLogEndpoint = cfg.CentralizedLogEndpoint ?? string.Empty;
                ConfigTheme = NormalizeThemePreference(cfg.ThemePreference);
                ConfigThemeDraft = ConfigTheme;

                ConfigEnableEncryptionDraft = cfg.EnableEncryption;
                ConfigCryptoSoftKeyDraft = (cfg.CryptoSoftKey ?? "").Trim();
                ConfigBusinessSoftwareDraft = (cfg.BusinessSoftware ?? "").Trim();
                NewBusinessSoftwareEntry = "";
                NewEncryptionExtension = "";
                NewPriorityExtension = "";
                SelectedBusinessSoftwareEntry = null;
                SelectedPriorityExtension = null;
                ConfigBusinessSoftwareEntriesDraft.Clear();
                EncryptionKeyCreationTraceDraft.Clear();

                foreach (var entry in ParseBusinessSoftwareEntries(ConfigBusinessSoftwareDraft))
                {
                    if (!ConfigBusinessSoftwareEntriesDraft.Any(e => string.Equals(e, entry, StringComparison.OrdinalIgnoreCase)))
                    {
                        ConfigBusinessSoftwareEntriesDraft.Add(entry);
                    }
                }

                ConfigEncryptionExtensionsDraft.Clear();
                if (cfg.EncryptionExtensions is not null)
                {
                    foreach (var extRaw in cfg.EncryptionExtensions)
                    {
                        var ext = (extRaw ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(ext)) continue;

                        if (!ext.StartsWith(".")) ext = "." + ext;
                        ext = ext.ToLowerInvariant();

                        if (!ConfigEncryptionExtensionsDraft.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                            ConfigEncryptionExtensionsDraft.Add(ext);
                    }
                }

                ConfigPriorityExtensionsDraft.Clear();
                if (cfg.PriorityExtensions is not null)
                {
                    foreach (var extRaw in cfg.PriorityExtensions)
                    {
                        var ext = (extRaw ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(ext)) continue;

                        if (!ext.StartsWith(".")) ext = "." + ext;
                        ext = ext.ToLowerInvariant();

                        if (!ConfigPriorityExtensionsDraft.Any(e => string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)))
                            ConfigPriorityExtensionsDraft.Add(ext);
                    }
                }

                if (cfg.EncryptionKeyCreationTrace is not null)
                {
                    foreach (var trace in cfg.EncryptionKeyCreationTrace)
                    {
                        string normalizedTrace = (trace ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(normalizedTrace)) continue;
                        EncryptionKeyCreationTraceDraft.Add(normalizedTrace);
                    }
                }

                Loc.SetLanguage(cfg.Language);
                RefreshBackupTypeOptions();
                RefreshThemeOptions();
                RefreshLogSortOptions();
                ApplyThemePreference(ConfigThemeDraft);
                UpdateBusinessSoftwareSuggestions();
                UpdateEncryptionExtensionSuggestions();
                UpdatePriorityExtensionSuggestions();
                RefreshLogFilesSummary();
                RefreshLogEntriesSummary();
                this.RaisePropertyChanged(nameof(PauseButtonText));
                ClearAreaMessage(MessageArea.Config);

                if (!string.IsNullOrWhiteSpace(SelectedLogFile))
                {
                    ViewSelectedLog();
                }
            }
            catch (Exception ex)
            {
                SetTimedAreaMessage(MessageArea.Config, string.Format(Loc["UiConfigLoadError"], ex.Message));
            }
        }

        private void SaveConfigFromViewModel()
        {
            try
            {
                var exts = BuildNormalizedEncryptionExtensionsDraft();
                var priorityExts = BuildNormalizedPriorityExtensionsDraft();

                var businessEntries = ConfigBusinessSoftwareEntriesDraft
                    .Select(e => NormalizeBusinessSoftwareEntry(e))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string businessSoftware = string.Join("; ", businessEntries);
                ConfigBusinessSoftwareDraft = businessSoftware;
                string themePreference = NormalizeThemePreference(ConfigThemeDraft);
                string cryptoSoftKey = (ConfigCryptoSoftKeyDraft ?? "").Trim();
                ConfigCryptoSoftKeyDraft = cryptoSoftKey;
                var keyTrace = EncryptionKeyCreationTraceDraft
                    .Select(e => (e ?? "").Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                string normalizedMode = Config.NormalizeLogDestinationMode(ConfigLogDestinationModeDraft);
                ConfigLogDestinationModeDraft = normalizedMode;

                string endpoint = (ConfigCentralizedLogEndpoint ?? string.Empty).Trim();
                if (Config.RequiresCentralizedEndpoint(normalizedMode))
                {
                    if (string.IsNullOrWhiteSpace(endpoint))
                    {
                        throw new InvalidOperationException(Loc["UiCentralizedEndpointRequired"]);
                    }

                    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    {
                        throw new InvalidOperationException(Loc["UiCentralizedEndpointInvalid"]);
                    }
                }
                ConfigCentralizedLogEndpoint = endpoint;

                _coreViewModel.model.UpdateConfig(
                    ConfigLogDirectory,
                    ConfigStateFilePath,
                    ConfigLanguageDraft,
                    enableEncryption: ConfigEnableEncryptionDraft,
                    encryptionExtensions: exts,
                    priorityExtensions: priorityExts,
                    cryptoSoftKey: cryptoSoftKey,
                    encryptionKeyCreationTrace: keyTrace,
                    businessSoftware: businessSoftware,
                    themePreference: themePreference,
                    logDestinationMode: normalizedMode,
                    centralizedLogEndpoint: endpoint);

                // Apply language to the current UI only after Save.
                ConfigLanguage = ConfigLanguageDraft;
                ConfigTheme = themePreference;
                ConfigThemeDraft = themePreference;
                Loc.SetLanguage(ConfigLanguageDraft);
                RefreshBackupTypeOptions();
                RefreshThemeOptions();
                RefreshLogSortOptions();
                ApplyThemePreference(themePreference);

                LoadConfigValuesFromModel();
                LoadLogs();
                SelectedLogFile = string.Empty;
                SelectedLogContent = Loc["UiSelectLogFile"];
                SetMessageFromCode(218, MessageArea.Config);
            }
            catch (Exception ex)
            {
                SetTimedAreaMessage(MessageArea.Config, string.Format(Loc["UiConfigSaveError"], ex.Message));
            }
        }

        private void LoadLogs()
        {
            try
            {
                ResetLogViewer();
                _allLogFiles.Clear();
                string previousSelection = SelectedLogFile;

                if (Directory.Exists(ConfigLogDirectory))
                {
                    var files = Directory.GetFiles(ConfigLogDirectory);
                    foreach (var f in files)
                        _allLogFiles.Add(Path.GetFileName(f));
                }

                ApplyLogFileFilterAndSort();

                if (LogFiles.Count == 0)
                {
                    LogSummaryText = Loc["UiNoLogsFound"];
                    SelectedLogContent = Loc["UiNoLogsFound"];
                    IsStructuredLogVisible = false;
                    IsRawLogVisible = true;
                    return;
                }

                LogSummaryText = Loc["UiSelectLogFile"];
                SelectedLogContent = Loc["UiSelectLogFile"];
                IsStructuredLogVisible = false;
                IsRawLogVisible = true;

                if (!string.IsNullOrWhiteSpace(previousSelection) &&
                    LogFiles.Any(f => string.Equals(f, previousSelection, StringComparison.Ordinal)))
                {
                    if (!string.Equals(SelectedLogFile, previousSelection, StringComparison.Ordinal))
                        SelectedLogFile = previousSelection;
                }
            }
            catch (Exception ex)
            {
                ResetLogViewer();
                _allLogFiles.Clear();
                LogFiles.Clear();
                RefreshLogFilesSummary();
                SelectedLogContent = string.Format(Loc["UiLogsLoadError"], ex.Message);
                LogSummaryText = Loc["UiLogs"];
                IsStructuredLogVisible = false;
                IsRawLogVisible = true;
            }
        }

        private void ResetLogViewer()
        {
            _allParsedLogEntries.Clear();
            ParsedLogEntries.Clear();
            SelectedParsedLogEntry = null;
            LogEntriesFilterSummary = string.Empty;
            IsStructuredLogVisible = false;
            IsRawLogVisible = true;
            LogSummaryText = "";
        }

        private void ViewSelectedLog()
        {
            ResetLogViewer();

            if (string.IsNullOrWhiteSpace(SelectedLogFile))
            {
                SelectedLogContent = Loc["UiSelectLogFile"];
                LogSummaryText = Loc["UiSelectLogFile"];
                IsRawLogVisible = true;
                return;
            }

            string path = Path.Combine(ConfigLogDirectory, SelectedLogFile);
            if (!File.Exists(path))
            {
                SelectedLogContent = Loc["UiLogFileMissing"];
                LogSummaryText = Loc["UiLogFileMissing"];
                IsRawLogVisible = true;
                return;
            }

            string raw = File.ReadAllText(path);

            if (TryParseStructuredLog(path, raw, out var entries))
            {
                _allParsedLogEntries.Clear();
                _allParsedLogEntries.AddRange(entries);

                LogSummaryText = string.Format(Loc["UiLogEntriesCount"], _allParsedLogEntries.Count, SelectedLogFile);
                IsStructuredLogVisible = true;
                IsRawLogVisible = false;
                SelectedLogContent = Loc["UiSelectLogEntry"];
                ApplyParsedLogEntryFilterAndSort(preserveSelection: false);
                return;
            }

            LogSummaryText = string.Format(Loc["UiLogRawPreview"], SelectedLogFile);
            SelectedLogContent = PrettyPrintRaw(path, raw);
            IsStructuredLogVisible = false;
            IsRawLogVisible = true;
        }

        private void UpdateSelectedLogEntryDetails()
        {
            if (SelectedParsedLogEntry is null)
            {
                SelectedLogContent = Loc["UiSelectLogEntry"];
                return;
            }

            SelectedLogContent = BuildLogEntryDetails(SelectedParsedLogEntry);
        }

        private static string NormalizeThemePreference(string? value)
        {
            string normalized = (value ?? "").Trim().ToLowerInvariant();
            return normalized switch
            {
                ThemeLight => ThemeLight,
                ThemeDark => ThemeDark,
                _ => ThemeAuto
            };
        }

        private void ApplyThemePreference(string? value)
        {
            if (Application.Current is null) return;

            string pref = NormalizeThemePreference(value);
            Application.Current.RequestedThemeVariant = pref switch
            {
                ThemeLight => ThemeVariant.Light,
                ThemeDark => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        private void RefreshThemeOptions()
        {
            string selectedKey = NormalizeThemePreference(ConfigThemeDraft);

            ThemeOptions.Clear();
            ThemeOptions.Add(new ThemeOptionItem(ThemeAuto, Loc["UiThemeAuto"]));
            ThemeOptions.Add(new ThemeOptionItem(ThemeLight, Loc["UiThemeLight"]));
            ThemeOptions.Add(new ThemeOptionItem(ThemeDark, Loc["UiThemeDark"]));

            SelectedThemeOption = ThemeOptions.FirstOrDefault(o => string.Equals(o.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
                ?? ThemeOptions.FirstOrDefault();
        }

        private void RefreshLogSortOptions()
        {
            string fileSortKey = SelectedLogSortOption?.Key ?? SortNewest;
            string entrySortKey = SelectedLogEntrySortOption?.Key ?? SortNewest;

            LogSortOptions.Clear();
            LogSortOptions.Add(new LogSortOptionItem(SortNewest, Loc["UiSortNewest"]));
            LogSortOptions.Add(new LogSortOptionItem(SortOldest, Loc["UiSortOldest"]));
            LogSortOptions.Add(new LogSortOptionItem(SortName, Loc["UiSortName"]));

            LogEntrySortOptions.Clear();
            LogEntrySortOptions.Add(new LogSortOptionItem(SortNewest, Loc["UiSortNewest"]));
            LogEntrySortOptions.Add(new LogSortOptionItem(SortOldest, Loc["UiSortOldest"]));
            LogEntrySortOptions.Add(new LogSortOptionItem(SortName, Loc["UiSortName"]));

            SelectedLogSortOption =
                LogSortOptions.FirstOrDefault(o => string.Equals(o.Key, fileSortKey, StringComparison.OrdinalIgnoreCase))
                ?? LogSortOptions.FirstOrDefault();

            SelectedLogEntrySortOption =
                LogEntrySortOptions.FirstOrDefault(o => string.Equals(o.Key, entrySortKey, StringComparison.OrdinalIgnoreCase))
                ?? LogEntrySortOptions.FirstOrDefault();
        }

        private void ApplyLogFileFilterAndSort(bool preserveSelection = true)
        {
            string previousSelection = preserveSelection ? (SelectedLogFile ?? "") : "";
            IEnumerable<string> query = _allLogFiles;

            string search = (LogFileSearchText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(f => f.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            string sortKey = SelectedLogSortOption?.Key ?? SortNewest;
            query = sortKey switch
            {
                SortOldest => query.OrderBy(f => f, StringComparer.OrdinalIgnoreCase),
                SortName => query.OrderBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
                                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase),
                _ => query.OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
            };

            var filtered = query.ToList();
            LogFiles.Clear();
            foreach (string file in filtered)
                LogFiles.Add(file);

            RefreshLogFilesSummary();

            if (!preserveSelection) return;

            if (!string.IsNullOrWhiteSpace(previousSelection) &&
                filtered.Any(f => string.Equals(f, previousSelection, StringComparison.Ordinal)))
            {
                if (!string.Equals(SelectedLogFile, previousSelection, StringComparison.Ordinal))
                    SelectedLogFile = previousSelection;
                return;
            }

            if (!string.IsNullOrWhiteSpace(SelectedLogFile) &&
                !filtered.Any(f => string.Equals(f, SelectedLogFile, StringComparison.Ordinal)))
            {
                SelectedLogFile = string.Empty;
            }
        }

        private void RefreshLogFilesSummary()
        {
            if (_allLogFiles.Count == 0)
            {
                LogFilesSummaryText = Loc["UiNoLogsFound"];
                return;
            }

            if (LogFiles.Count == _allLogFiles.Count)
            {
                LogFilesSummaryText = string.Format(Loc["UiLogFilesCount"], LogFiles.Count);
                return;
            }

            LogFilesSummaryText = string.Format(Loc["UiLogFilesFilteredCount"], LogFiles.Count, _allLogFiles.Count);
        }

        private void ApplyParsedLogEntryFilterAndSort(bool preserveSelection = true)
        {
            var previousSelection = preserveSelection ? SelectedParsedLogEntry : null;
            IEnumerable<LogEntryViewItem> query = _allParsedLogEntries;

            string search = (LogEntrySearchText ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(entry => MatchesLogSearch(entry, search));
            }

            if (ShowOnlyFailedLogEntries)
            {
                query = query.Where(entry => entry.TransferTimeMs < 0);
            }

            string sortKey = SelectedLogEntrySortOption?.Key ?? SortNewest;
            query = sortKey switch
            {
                SortOldest => query.OrderBy(entry => ParseLogTimeOrMin(entry.Time))
                                   .ThenBy(entry => entry.BackupName, StringComparer.OrdinalIgnoreCase),
                SortName => query.OrderBy(entry => entry.BackupName, StringComparer.OrdinalIgnoreCase)
                                 .ThenByDescending(entry => ParseLogTimeOrMin(entry.Time)),
                _ => query.OrderByDescending(entry => ParseLogTimeOrMin(entry.Time))
                          .ThenBy(entry => entry.BackupName, StringComparer.OrdinalIgnoreCase)
            };

            var filtered = query.ToList();

            ParsedLogEntries.Clear();
            foreach (var entry in filtered)
                ParsedLogEntries.Add(entry);

            RefreshLogEntriesSummary();

            if (!IsStructuredLogVisible) return;

            if (ParsedLogEntries.Count == 0)
            {
                SelectedParsedLogEntry = null;
                SelectedLogContent = Loc["UiNoMatchingLogEntries"];
                return;
            }

            if (previousSelection is not null && ParsedLogEntries.Contains(previousSelection))
            {
                if (!ReferenceEquals(SelectedParsedLogEntry, previousSelection))
                    SelectedParsedLogEntry = previousSelection;
                return;
            }

            if (SelectedParsedLogEntry is null || !ParsedLogEntries.Contains(SelectedParsedLogEntry))
                SelectedParsedLogEntry = ParsedLogEntries.FirstOrDefault();
        }

        private void RefreshLogEntriesSummary()
        {
            if (_allParsedLogEntries.Count == 0)
            {
                LogEntriesFilterSummary = string.Empty;
                return;
            }

            if (ParsedLogEntries.Count == _allParsedLogEntries.Count)
            {
                LogEntriesFilterSummary = string.Format(Loc["UiLogEntriesShownCount"], ParsedLogEntries.Count);
                return;
            }

            LogEntriesFilterSummary = string.Format(Loc["UiLogEntriesFilteredCount"], ParsedLogEntries.Count, _allParsedLogEntries.Count);
        }

        private static bool MatchesLogSearch(LogEntryViewItem entry, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;

            return (entry.BackupName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (entry.Time ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (entry.SourcePath ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || (entry.TargetPath ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                || entry.FileSizeBytes.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase)
                || entry.TransferTimeMs.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase)
                || entry.EncryptionTimeMs.ToString(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime ParseLogTimeOrMin(string rawTime)
        {
            if (DateTime.TryParse(rawTime, out var parsed))
                return parsed;

            if (DateTime.TryParse(rawTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
                return parsed;

            return DateTime.MinValue;
        }

        private bool TryParseStructuredLog(string path, string raw, out List<LogEntryViewItem> entries)
        {
            entries = new List<LogEntryViewItem>();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string trimmed = raw.TrimStart();
            string ext = (Path.GetExtension(path) ?? "").Trim().ToLowerInvariant();

            if (ext == ".json" || trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            {
                return TryParseJsonLogEntries(raw, out entries);
            }

            if (ext == ".xml" || trimmed.StartsWith("<"))
            {
                return TryParseXmlLogEntries(raw, out entries);
            }

            return false;
        }

        private static bool TryParseJsonLogEntries(string raw, out List<LogEntryViewItem> entries)
        {
            entries = new List<LogEntryViewItem>();

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return false;

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object) continue;

                    entries.Add(new LogEntryViewItem
                    {
                        BackupName = FirstNonEmpty(
                            TryGetJsonString(element, "Name"),
                            TryGetJsonString(element, "BackupName")),
                        SourcePath = FirstNonEmpty(
                            TryGetJsonString(element, "FileSource"),
                            TryGetJsonString(element, "SourcePath")),
                        TargetPath = FirstNonEmpty(
                            TryGetJsonString(element, "FileTarget"),
                            TryGetJsonString(element, "DestinationPath")),
                        MachineName = TryGetJsonString(element, "MachineName"),
                        UserName = TryGetJsonString(element, "UserName"),
                        FileSizeBytes = TryGetJsonLongAny(element, "FileSize"),
                        TransferTimeMs = TryGetJsonLongAny(element, "FileTransferTime", "TransferTimeMs"),
                        EncryptionTimeMs = TryGetJsonLongAny(element, "EncryptionTime", "EncryptionTimeMs"),
                        Time = FirstNonEmpty(
                            TryGetJsonString(element, "time"),
                            TryGetJsonString(element, "Timestamp"))
                    });
                }

                return entries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseXmlLogEntries(string raw, out List<LogEntryViewItem> entries)
        {
            entries = new List<LogEntryViewItem>();

            try
            {
                var doc = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);
                var logs = doc.Root?.Elements("Log");
                if (logs is null) return false;

                foreach (var log in logs)
                {
                    entries.Add(new LogEntryViewItem
                    {
                        BackupName = (log.Element("Name")?.Value ?? "").Trim(),
                        SourcePath = (log.Element("FileSource")?.Value ?? "").Trim(),
                        TargetPath = (log.Element("FileTarget")?.Value ?? "").Trim(),
                        MachineName = (log.Element("MachineName")?.Value ?? "").Trim(),
                        UserName = (log.Element("UserName")?.Value ?? "").Trim(),
                        FileSizeBytes = TryGetXmlLong(log, "FileSize"),
                        TransferTimeMs = TryGetXmlLong(log, "FileTransferTime"),
                        EncryptionTimeMs = TryGetXmlLong(log, "EncryptionTime"),
                        Time = (log.Element("Time")?.Value ?? "").Trim()
                    });
                }

                return entries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private string BuildLogEntryDetails(LogEntryViewItem entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Loc["UiLogTime"]}: {entry.Time}");
            sb.AppendLine($"{Loc["Name"]}: {entry.BackupName}");
            sb.AppendLine($"{Loc["Source"]}: {entry.SourcePath}");
            sb.AppendLine($"{Loc["Destination"]}: {entry.TargetPath}");
            sb.AppendLine($"{Loc["UiLogMachine"]}: {entry.MachineName}");
            sb.AppendLine($"{Loc["UiLogUser"]}: {entry.UserName}");
            sb.AppendLine($"{Loc["UiLogFileSize"]}: {entry.FileSizeBytes} B");
            sb.AppendLine($"{Loc["UiLogTransferTime"]}: {entry.TransferTimeMs} ms");
            sb.AppendLine($"{Loc["UiLogEncryptionTime"]}: {entry.EncryptionTimeMs} ms");
            return sb.ToString().TrimEnd();
        }

        private static string PrettyPrintRaw(string path, string? raw)
        {
            string safeRaw = raw ?? string.Empty;
            string trimmed = safeRaw.TrimStart();
            string ext = (Path.GetExtension(path) ?? "").Trim().ToLowerInvariant();

            if (ext == ".json" || trimmed.StartsWith("[") || trimmed.StartsWith("{"))
                return PrettyPrintJson(safeRaw);

            if (ext == ".xml" || trimmed.StartsWith("<"))
                return PrettyPrintXml(safeRaw);

            return safeRaw;
        }

        private static string PrettyPrintJson(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return JsonSerializer.Serialize(
                    doc.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
            }
            catch
            {
                return raw;
            }
        }

        private static string PrettyPrintXml(string raw)
        {
            try
            {
                var doc = XDocument.Parse(raw);
                return doc.ToString();
            }
            catch
            {
                return raw;
            }
        }

        private static string[] ParseBusinessSoftwareEntries(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();

            return raw
                .Split(new[] { ';', ',', '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeBusinessSoftwareEntry)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToArray();
        }

        private static string TryGetJsonString(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var prop))
                return string.Empty;

            return prop.ValueKind switch
            {
                JsonValueKind.String => prop.GetString() ?? string.Empty,
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => prop.GetRawText()
            };
        }

        private static long TryGetJsonLong(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var prop))
                return 0;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out long val))
                return val;

            if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out long parsed))
                return parsed;

            return 0;
        }

        private static long TryGetJsonLongAny(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (!obj.TryGetProperty(name, out var prop))
                    continue;

                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out long val))
                    return val;

                if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out long parsed))
                    return parsed;
            }

            return 0;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static long TryGetXmlLong(XElement node, string elementName)
        {
            if (node.Element(elementName) is null) return 0;
            return long.TryParse(node.Element(elementName)!.Value, out var val) ? val : 0;
        }

        private void RefreshBackupTypeOptions()
        {
            var currentType = SelectedBackupTypeItem?.Type;

            BackupTypeOptions.Clear();
            BackupTypeOptions.Add(new BackupTypeItem(BackupType.FULL, Loc["FullBackup"]));
            BackupTypeOptions.Add(new BackupTypeItem(BackupType.DIFFERENTIAL, Loc["DifferentialBackup"]));

            SelectedBackupTypeItem =
                BackupTypeOptions.FirstOrDefault(i => i.Type == currentType)
                ?? BackupTypeOptions.FirstOrDefault(i => i.Type == BackupType.FULL);
        }

        // Type utilisé par le ComboBox
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
