using easySave_BMT.Model_;
using ReactiveUI;
using System;
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
                ConfigBusinessSoftwareDraft = (cfg.BusinessSoftware ?? "").Trim();
                NewBusinessSoftwareEntry = "";
                SelectedBusinessSoftwareEntry = null;
                ConfigBusinessSoftwareEntriesDraft.Clear();

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

                Loc.SetLanguage(cfg.Language);
                RefreshBackupTypeOptions();
                this.RaisePropertyChanged(nameof(PauseButtonText));
                ClearAreaMessage(MessageArea.Config);
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
                var exts = ConfigEncryptionExtensionsDraft
                    .Select(e => (e ?? "").Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var businessEntries = ConfigBusinessSoftwareEntriesDraft
                    .Select(e => NormalizeBusinessSoftwareEntry(e))
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string businessSoftware = string.Join("; ", businessEntries);
                ConfigBusinessSoftwareDraft = businessSoftware;

                _coreViewModel.model.UpdateConfig(
                    ConfigLogDirectory,
                    ConfigStateFilePath,
                    ConfigLanguageDraft,
                    enableEncryption: ConfigEnableEncryptionDraft,
                    encryptionExtensions: exts,
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

                if (Directory.Exists(ConfigLogDirectory))
                {
                    var files = Directory.GetFiles(ConfigLogDirectory).OrderByDescending(f => f);
                    foreach (var f in files)
                        LogFiles.Add(Path.GetFileName(f));
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
            if (File.Exists(path))
                SelectedLogContent = FormatLogForDisplay(path, File.ReadAllText(path));
            else
                SelectedLogContent = Loc["UiLogFileMissing"];
        }

        private static string FormatLogForDisplay(string path, string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string trimmed = raw.TrimStart();
            string ext = (Path.GetExtension(path) ?? "").Trim().ToLowerInvariant();

            if (ext == ".json" || trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            {
                return TryBuildReadableJsonLog(path, raw, out string formattedJson)
                    ? formattedJson
                    : PrettyPrintJson(raw);
            }

            if (ext == ".xml" || trimmed.StartsWith("<"))
            {
                return TryBuildReadableXmlLog(path, raw, out string formattedXml)
                    ? formattedXml
                    : PrettyPrintXml(raw);
            }

            return raw;
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

        private static bool TryBuildReadableJsonLog(string path, string raw, out string output)
        {
            output = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return false;

                var entries = doc.RootElement.EnumerateArray().ToList();
                var sb = new StringBuilder();
                sb.AppendLine($"File: {Path.GetFileName(path)}");
                sb.AppendLine($"Entries: {entries.Count}");
                sb.AppendLine(new string('-', 64));

                for (int i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];
                    if (e.ValueKind != JsonValueKind.Object) continue;

                    string name = TryGetJsonString(e, "Name");
                    string time = TryGetJsonString(e, "time");
                    string source = TryGetJsonString(e, "FileSource");
                    string target = TryGetJsonString(e, "FileTarget");
                    long size = TryGetJsonLong(e, "FileSize");
                    long transfer = TryGetJsonLong(e, "FileTransferTime");
                    long encryption = TryGetJsonLong(e, "EncryptionTime");

                    sb.AppendLine($"[{i + 1}] {time} | Backup: {name}");
                    sb.AppendLine($"Source : {source}");
                    sb.AppendLine($"Target : {target}");
                    sb.AppendLine($"Size   : {size} bytes");
                    sb.AppendLine($"Times  : copy={transfer} ms, encrypt={encryption} ms");
                    sb.AppendLine();
                }

                output = sb.ToString().TrimEnd();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryBuildReadableXmlLog(string path, string raw, out string output)
        {
            output = string.Empty;

            try
            {
                var doc = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);
                var logs = doc.Root?.Elements("Log").ToList();
                if (logs is null || logs.Count == 0)
                    return false;

                var sb = new StringBuilder();
                sb.AppendLine($"File: {Path.GetFileName(path)}");
                sb.AppendLine($"Entries: {logs.Count}");
                sb.AppendLine(new string('-', 64));

                for (int i = 0; i < logs.Count; i++)
                {
                    var e = logs[i];
                    string name = (e.Element("Name")?.Value ?? "").Trim();
                    string time = (e.Element("Time")?.Value ?? "").Trim();
                    string source = (e.Element("FileSource")?.Value ?? "").Trim();
                    string target = (e.Element("FileTarget")?.Value ?? "").Trim();
                    string size = (e.Element("FileSize")?.Value ?? "0").Trim();
                    string transfer = (e.Element("FileTransferTime")?.Value ?? "0").Trim();
                    string encryption = (e.Element("EncryptionTime")?.Value ?? "0").Trim();

                    sb.AppendLine($"[{i + 1}] {time} | Backup: {name}");
                    sb.AppendLine($"Source : {source}");
                    sb.AppendLine($"Target : {target}");
                    sb.AppendLine($"Size   : {size} bytes");
                    sb.AppendLine($"Times  : copy={transfer} ms, encrypt={encryption} ms");
                    sb.AppendLine();
                }

                output = sb.ToString().TrimEnd();
                return true;
            }
            catch
            {
                return false;
            }
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
