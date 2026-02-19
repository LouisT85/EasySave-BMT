using easySave_BMT.Model_;
using System;
using System.IO;
using System.Linq;

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

                _coreViewModel.model.UpdateConfig(
                    ConfigLogDirectory,
                    ConfigStateFilePath,
                    ConfigLanguageDraft,
                    enableEncryption: ConfigEnableEncryptionDraft,
                    encryptionExtensions: exts);

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
                SelectedLogContent = File.ReadAllText(path);
            else
                SelectedLogContent = Loc["UiLogFileMissing"];
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
