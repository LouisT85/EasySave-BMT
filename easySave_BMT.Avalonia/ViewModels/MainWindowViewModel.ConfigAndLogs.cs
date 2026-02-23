using easySave_BMT.Model_;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
        private System.Collections.Generic.List<string> BuildNormalizedEncryptionExtensionsDraft()
        {
            return ConfigEncryptionExtensionsDraft
                .Select(e => (e ?? "").Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ApplyEncryptionDraftToModelForLaunch()
        {
            var cfg = _coreViewModel.model.GetConfig();
            var exts = BuildNormalizedEncryptionExtensionsDraft();
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

                ConfigEnableEncryptionDraft = cfg.EnableEncryption;
                ConfigCryptoSoftKeyDraft = (cfg.CryptoSoftKey ?? "").Trim();
                ConfigBusinessSoftwareDraft = (cfg.BusinessSoftware ?? "").Trim();
                NewBusinessSoftwareEntry = "";
                SelectedBusinessSoftwareEntry = null;
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

                var businessEntries = ConfigBusinessSoftwareEntriesDraft
                    .Select(e => NormalizeBusinessSoftwareEntry(e))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string businessSoftware = string.Join("; ", businessEntries);
                ConfigBusinessSoftwareDraft = businessSoftware;
                string cryptoSoftKey = (ConfigCryptoSoftKeyDraft ?? "").Trim();
                ConfigCryptoSoftKeyDraft = cryptoSoftKey;
                var keyTrace = EncryptionKeyCreationTraceDraft
                    .Select(e => (e ?? "").Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();

                _coreViewModel.model.UpdateConfig(
                    ConfigLogDirectory,
                    ConfigStateFilePath,
                    ConfigLanguageDraft,
                    enableEncryption: ConfigEnableEncryptionDraft,
                    encryptionExtensions: exts,
                    cryptoSoftKey: cryptoSoftKey,
                    encryptionKeyCreationTrace: keyTrace,
                    businessSoftware: businessSoftware);

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
                SetTimedAreaMessage(MessageArea.Config, string.Format(Loc["UiConfigSaveError"], ex.Message));
            }
        }

        private void LoadLogs()
        {
            try
            {
                LogFiles.Clear();
                ResetLogViewer();

                if (Directory.Exists(ConfigLogDirectory))
                {
                    var files = Directory.GetFiles(ConfigLogDirectory).OrderByDescending(f => f);
                    foreach (var f in files)
                        LogFiles.Add(Path.GetFileName(f));
                }

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
            }
            catch (Exception ex)
            {
                ResetLogViewer();
                SelectedLogContent = string.Format(Loc["UiLogsLoadError"], ex.Message);
                LogSummaryText = Loc["UiLogs"];
                IsStructuredLogVisible = false;
                IsRawLogVisible = true;
            }
        }

        private void ResetLogViewer()
        {
            ParsedLogEntries.Clear();
            SelectedParsedLogEntry = null;
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
                foreach (var entry in entries)
                    ParsedLogEntries.Add(entry);

                LogSummaryText = string.Format(Loc["UiLogEntriesCount"], ParsedLogEntries.Count, SelectedLogFile);
                IsStructuredLogVisible = true;
                IsRawLogVisible = false;
                SelectedLogContent = Loc["UiSelectLogEntry"];
                SelectedParsedLogEntry = ParsedLogEntries.FirstOrDefault();
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
                        BackupName = TryGetJsonString(element, "Name"),
                        SourcePath = TryGetJsonString(element, "FileSource"),
                        TargetPath = TryGetJsonString(element, "FileTarget"),
                        FileSizeBytes = TryGetJsonLong(element, "FileSize"),
                        TransferTimeMs = TryGetJsonLong(element, "FileTransferTime"),
                        EncryptionTimeMs = TryGetJsonLong(element, "EncryptionTime"),
                        Time = TryGetJsonString(element, "time")
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
