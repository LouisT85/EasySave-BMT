using Avalonia.Threading;
using easySave_BMT.Model_;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel
    {
        private const string EtaToken = " | ETA ";

        private static (string BaseName, string? Eta) SplitBackupNameAndEta(string backupName)
        {
            if (string.IsNullOrWhiteSpace(backupName))
                return ("", null);
 
            int idx = backupName.IndexOf(EtaToken, StringComparison.Ordinal);
            if (idx <= 0)
                return (backupName, null);

            string baseName = backupName.Substring(0, idx).Trim();
            string eta = backupName.Substring(idx + EtaToken.Length).Trim();

            return (baseName, string.IsNullOrWhiteSpace(eta) ? null : eta);
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
                        SetTimedDashboardStatusText(string.Format(Loc["UiListUpdated"], _coreViewModel.model.saves.Count));
                    else
                        SetTimedDashboardStatusText(Loc["UiNoBackupsDefined"]);
                }
            }

            if (selectedNames.Count == 0)
            {
                SelectedSaves.Clear();
                SelectedSave = null;
                return;
            }

            SelectedSaves.Clear();
            foreach (var name in selectedNames)
            {
                var match = Saves.FirstOrDefault(s => string.Equals(s.name, name, StringComparison.Ordinal));
                if (match is not null) SelectedSaves.Add(match);
            }

            SelectedSave =
                SelectedSaves.OfType<Model_.Save>().FirstOrDefault()
                ?? Saves.FirstOrDefault(s => string.Equals(s.name, selectedNames[0], StringComparison.Ordinal));
        }

        private void AddSave()
        {
            if (string.IsNullOrWhiteSpace(NewSaveName) ||
                string.IsNullOrWhiteSpace(NewSaveSourcePath) ||
                string.IsNullOrWhiteSpace(NewSaveDestinationPath))
            {
                SetTimedAreaMessage(MessageArea.NewTask, Loc["UiFillAllFields"], "");
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
                ListSaves(showUserFeedback: false);

                NewSaveName = "";
                NewSaveSourcePath = "";
                NewSaveDestinationPath = "";
            }
        }

        private void RemoveSave()
        {
            var names = GetSelectedSaveNames();
            if (names.Count == 0)
            {
                SetTimedAreaMessage(MessageArea.Dashboard, Loc["UiSelectBackup"], "");
                return;
            }

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
            SetTimedDashboardStatusText("");
        }

        private async Task LaunchBackupAsync()
        {
            var names = GetSelectedSaveNames();
            if (names.Count == 0)
            {
                SetTimedAreaMessage(MessageArea.Dashboard, Loc["UiSelectBackup"], "");
                return;
            }

            ProgressPercent = 0;
            IsProgressVisible = true;
            IsBackupRunning = true;
            _coreViewModel.model.ClearStopRequest();
            _coreViewModel.model.ClearPauseRequest();
            IsBackupPaused = false;

            int lastResult = 0;
            bool stoppedOrBlocked = false;

            try
            {
                ApplyEncryptionDraftToModelForLaunch();
                ListSaves(showUserFeedback: false);

                var toRun = _coreViewModel.model.saves.Where(s => names.Contains(s.name)).ToList();
                if (toRun.Count == 0)
                {
                    SetTimedAreaMessage(MessageArea.Dashboard, Loc["UiSelectBackup"], "");
                    return;
                }

                foreach (var save in toRun)
                {
                    SetSaveUiProgress(save.name, 0);
                }

                string launchLabel = toRun.Count == 1 ? toRun[0].name : $"{toRun.Count} backups";
                SetTimedAreaMessage(MessageArea.Dashboard, string.Format(Loc["UiLaunchingBackup"], launchLabel), "");

                var batchResults = await Task.Run(() => _coreViewModel.backupLauncher.LaunchBackupsInParallel(toRun));
                if (batchResults.Count == 0)
                {
                    SetTimedAreaMessage(MessageArea.Dashboard, Loc["UiSelectBackup"], "");
                    return;
                }

                bool sawCode104 = false;
                bool sawCode105 = false;
                bool sawCode216 = false;
                int firstInvalidResult = 0;

                foreach (var result in batchResults)
                {
                    int code = result.ResultCode;
                    lastResult = code;

                    if (code == 104)
                    {
                        sawCode104 = true;
                        _coreViewModel.model.FinishBackup(result.Save);
                        SetSaveUiProgress(result.Save.name, 100);
                        continue;
                    }

                    if (code == 105)
                    {
                        sawCode105 = true;
                        _coreViewModel.model.FinishBackup(result.Save);
                        SetSaveUiProgress(result.Save.name, 100);
                        continue;
                    }

                    if (code == 216)
                    {
                        sawCode216 = true;
                        _coreViewModel.model.FinishBackup(result.Save);
                        SetSaveUiProgress(result.Save.name, 100);
                        continue;
                    }

                    if (firstInvalidResult == 0)
                    {
                        firstInvalidResult = code;
                    }
                }

                if (TryHandleConsumedStopInfo(toRun))
                {
                    stoppedOrBlocked = true;
                }
                else if (firstInvalidResult != 0)
                {
                    stoppedOrBlocked = true;
                    lastResult = firstInvalidResult;
                    SetMessageFromCode(lastResult, MessageArea.Dashboard);
                }
                else if (sawCode216)
                {
                    lastResult = 216;
                }
                else if (!sawCode104 && sawCode105)
                {
                    lastResult = 105;
                }
                else
                {
                    lastResult = 104;
                }
            }
            catch (Exception ex)
            {
                SetTimedAreaMessage(MessageArea.Dashboard, string.Format(Loc["UiBackupException"], ex.Message), "");
                return;
            }
            finally
            {
                IsBackupRunning = false;
                IsBackupPaused = false;
                _coreViewModel.model.ClearPauseRequest();
            }

            ListSaves(showUserFeedback: false);

            if (!stoppedOrBlocked)
            {
                SetMessageFromCode(lastResult, MessageArea.Dashboard);
            }

            if (!stoppedOrBlocked && names.Count > 1)
            {
                string done = Loc["UiBackupsFinished"];
                if (string.IsNullOrWhiteSpace(DashboardStatusText))
                {
                    SetTimedDashboardStatusText(done);
                }
                else if (!DashboardStatusText.Contains(done, StringComparison.Ordinal))
                {
                    DashboardStatusText = DashboardStatusText.TrimEnd() + "\n" + done;
                }
            }
            else if (!stoppedOrBlocked && lastResult == 105)
            {
                SetTimedDashboardStatusText("");
            }
        }

        private bool TryHandleConsumedStopInfo(IReadOnlyList<Model_.Save> saves)
        {
            if (!_coreViewModel.model.TryConsumeStopInfo(out var stopReason, out var stopDetail))
                return false;

            string displayName = saves.FirstOrDefault()?.name ?? "";

            if (stopReason == BackupStopReason.BusinessSoftwareDetected)
            {
                string spec = string.IsNullOrWhiteSpace(stopDetail) ? _coreViewModel.model.GetBusinessSoftwareSpec() : stopDetail;
                DashboardMessage = string.Format(Loc["UiBackupStoppedByBusiness"], displayName, spec);
            }
            else if (stopReason == BackupStopReason.UserRequested)
            {
                DashboardMessage = string.Format(Loc["UiBackupStoppedByUser"], displayName);
                foreach (var save in saves)
                {
                    SetSaveUiProgress(save.name, 0);
                }
            }

            ProgressText = "";
            ProgressPercent = 0;
            IsProgressVisible = false;
            return true;
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

        private List<string> GetSelectedSaveNames()
        {
            var names = SelectedSaves.OfType<Model_.Save>().Select(s => s.name).Distinct().ToList();
            if (SelectedSave is not null && !names.Contains(SelectedSave.name))
            {
                names.Add(SelectedSave.name);
            }
            return names;
        }

        // --- IProgressObserverGUI Implementation ---
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            var (baseName, eta) = SplitBackupNameAndEta(backupName);

            Dispatcher.UIThread.Post(() =>
            {
                ProgressPercent = Math.Clamp(percent, 0, 100);

                string etaPart = string.IsNullOrWhiteSpace(eta) ? "" : $" | ETA: {eta}";
                ProgressText = $"{baseName}: {percent}% ({Loc["FilesRemaining"]}: {filesLeft}){etaPart}";

                IsProgressVisible = true;
                SetSaveUiProgress(baseName, percent);
            });
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            var (baseName, _) = SplitBackupNameAndEta(backupName);

            Dispatcher.UIThread.Post(() =>
            {
                SetSaveUiProgress(baseName, 100);

                ProgressPercent = 100;
                IsProgressVisible = true;

                double seconds = transferTime / 1000.0;
                SetTimedDashboardStatusText(string.Format(Loc["UiBackupFinished"], baseName, seconds));
            });
        }

        public void OnFileError(string fileName)
        {
            Dispatcher.UIThread.Post(() =>
                SetTimedDashboardStatusText($"{Loc["CopyFailed"]}: {fileName}")
            );
        }

        public void OnEncryptionSummary(string backupName, int encryptedCount)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (encryptedCount <= 0) return;

                var (baseName, _) = SplitBackupNameAndEta(backupName);
                string summary = string.Format(Loc["UiEncryptionSummarySimple"], baseName, encryptedCount);

                if (string.IsNullOrWhiteSpace(DashboardStatusText))
                    DashboardStatusText = summary;
                else
                    DashboardStatusText = DashboardStatusText.TrimEnd() + "\n" + summary;
            });
        }

        public void ShowMessage(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (SelectedTabIndex == 1)
                    SetTimedAreaMessage(MessageArea.NewTask, message);
                else if (SelectedTabIndex == 3)
                    SetTimedAreaMessage(MessageArea.Config, message);
                else
                    SetTimedAreaMessage(MessageArea.Dashboard, message);
            });
        }
    }
}
