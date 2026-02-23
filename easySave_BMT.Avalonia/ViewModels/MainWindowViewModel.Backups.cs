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

                // Reset champs uniquement en cas de succes
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
                // Reload from file so a task still runs correctly after closing/reopening the app.
                ListSaves(showUserFeedback: false);

                var toRun = _coreViewModel.model.saves.Where(s => names.Contains(s.name)).ToList();
                if (toRun.Count == 0)
                {
                    SetTimedAreaMessage(MessageArea.Dashboard, Loc["UiSelectBackup"], "");
                    return;
                }

                foreach (var save in toRun)
                {
                    // Reset per-save progress at the start of each run.
                    SetSaveUiProgress(save.name, 0);

                    SetTimedAreaMessage(MessageArea.Dashboard, string.Format(Loc["UiLaunchingBackup"], save.name), "");
                    lastResult = await Task.Run(() => _coreViewModel.backupLauncher.LaunchBackupType(save));

                    // Stop batch execution if a stop/block was requested (user or business software).
                    if (_coreViewModel.model.TryConsumeStopInfo(out var stopReason, out var stopDetail))
                    {
                        if (stopReason == BackupStopReason.BusinessSoftwareDetected)
                        {
                            string spec = string.IsNullOrWhiteSpace(stopDetail) ? _coreViewModel.model.GetBusinessSoftwareSpec() : stopDetail;
                            DashboardMessage = string.Format(Loc["UiBackupStoppedByBusiness"], save.name, spec);
                        }
                        else if (stopReason == BackupStopReason.UserRequested)
                        {
                            DashboardMessage = string.Format(Loc["UiBackupStoppedByUser"], save.name);

                            // User stop cleans up the destination folder, so reset UI progress for this job.
                            SetSaveUiProgress(save.name, 0);
                        }

                        _coreViewModel.model.FinishBackup(save);
                        ProgressText = "";
                        ProgressPercent = 0;
                        IsProgressVisible = false;
                        stoppedOrBlocked = true;
                        break;
                    }

                    if (lastResult == 104 || lastResult == 105 || lastResult == 216)
                    {
                        _coreViewModel.model.FinishBackup(save);
                        SetSaveUiProgress(save.name, 100);
                    }
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

            // Update last backup dates before showing the final result message.
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
                // Differential backup with no changes: avoid confusing status text.
                SetTimedDashboardStatusText("");
            }
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
            Dispatcher.UIThread.Post(() =>
            {
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
                SetSaveUiProgress(backupName, 100);

                ProgressPercent = 100;
                IsProgressVisible = true;
                SetTimedDashboardStatusText(string.Format(Loc["UiBackupFinished"], backupName, transferTime));
            });
        }

        public void OnFileError(string fileName)
        {
            Dispatcher.UIThread.Post(() =>
                SetTimedDashboardStatusText($"{Loc["CopyFailed"]}: {fileName}")
            );
        }

        public void OnEncryptionSummary(string backupName, int encryptedCount, int skippedAlreadyEncryptedCount)
        {
            Dispatcher.UIThread.Post(() =>
            {
                string? summary = null;

                if (encryptedCount <= 0 && skippedAlreadyEncryptedCount > 0)
                {
                    summary = string.Format(Loc["UiAllFilesAlreadyEncrypted"], backupName);
                }
                else if (encryptedCount > 0 && skippedAlreadyEncryptedCount > 0)
                {
                    summary = string.Format(Loc["UiEncryptionSummary"], backupName, encryptedCount, skippedAlreadyEncryptedCount);
                }

                if (string.IsNullOrWhiteSpace(summary)) return;

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
                // Route generic core messages to the currently visible tab.
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
