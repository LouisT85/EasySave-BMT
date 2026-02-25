using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using easySave_BMT.Model_;
using easySave_BMT.Resources_;

namespace easySave_BMT.ViewModel_.Backup
{
    /// <summary>
    /// Executes backup jobs (full/differential), handles pause/stop, and reports progress to console/GUI.
    /// </summary>
    public class BackupLauncher
    {
        public enum FileSelectionMode
        {
            All = 0,
            PriorityOnly = 1,
            NonPriorityOnly = 2
        }

        public readonly struct FilePriorityCounts
        {
            public int TotalFiles { get; }
            public int PriorityFiles { get; }
            public int NonPriorityFiles { get; }

            public FilePriorityCounts(int totalFiles, int priorityFiles, int nonPriorityFiles)
            {
                TotalFiles = totalFiles;
                PriorityFiles = priorityFiles;
                NonPriorityFiles = nonPriorityFiles;
            }
        }

        public readonly struct BackupBatchItemResult
        {
            public Save Save { get; }
            public int ResultCode { get; }

            public BackupBatchItemResult(Save save, int resultCode)
            {
                Save = save;
                ResultCode = resultCode;
            }
        }

        private const string EasySaveCryptoMagicV1 = "EASYSAVECRYPT1";
        private const string EasySaveCryptoMagicV2 = "EASYSAVECRYPT2";

        private readonly ViewModel _viewModel;

        public BackupLauncher(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void LaunchBackupsave()
        {
            if (_viewModel.model.saves.Count > 0)
            {
                int userChoice = _viewModel.view.LaunchBackupChoice();

                switch (userChoice)
                {
                    case 0:
                        return;

                    case 1:
                        BackupAllSaves();
                        break;

                    default:
                        BackupSingleSave(userChoice);
                        break;
                }

                _viewModel.view.DisplayMessage(1);
            }
            else
            {
                _viewModel.view.DisplayMessage(204);
            }
        }

        public int LaunchBackupType(Save _save)
        {
            return LaunchBackupType(_save, FileSelectionMode.All, allowResumeFromCompletedState: false);
        }

        public int LaunchBackupType(
            Save _save,
            FileSelectionMode selectionMode,
            bool allowResumeFromCompletedState = false)
        {
            DirectoryInfo dir = new DirectoryInfo(_save.src);

            if (!dir.Exists || !Directory.Exists(_save.dst))
            {
                return 207;
            }

            try
            {
                string srcFull = Path.GetFullPath(_save.src).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string dstFull = Path.GetFullPath(_save.dst).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (string.Equals(srcFull, dstFull, StringComparison.OrdinalIgnoreCase))
                {
                    return 212;
                }

                if (dstFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
                {
                    return 217;
                }
            }
            catch
            {
                // Intentionally ignored; the copy layer will surface errors.
            }

            if (_save.state is null || (!allowResumeFromCompletedState && _save.state.progress >= 100))
            {
                _save.state = new State(0, 0, _save.src, _save.dst);
            }

            _viewModel.model.UpdateSaveState(_save);

            return ExecuteBackupStrategy(_save, dir, selectionMode, allowResumeFromCompletedState);
        }

        public bool HasPriorityExtensionsConfigured()
        {
            return GetConfiguredPriorityExtensions().Count > 0;
        }

        public FilePriorityCounts GetFilePriorityCounts(Save save)
        {
            if (save is null)
                return new FilePriorityCounts(0, 0, 0);

            var dir = new DirectoryInfo(save.src ?? "");
            if (!dir.Exists || !Directory.Exists(save.dst))
                return new FilePriorityCounts(0, 0, 0);

            if (!TryGetCandidateFiles(save, dir, out var candidates, out _))
                return new FilePriorityCounts(0, 0, 0);

            var priorityExtensions = GetConfiguredPriorityExtensions();
            if (priorityExtensions.Count == 0)
                return new FilePriorityCounts(candidates.Length, 0, candidates.Length);

            int priority = 0;
            int nonPriority = 0;

            foreach (var file in candidates)
            {
                if (IsPriorityExtension(Path.GetExtension(file.FullName), priorityExtensions))
                    priority++;
                else
                    nonPriority++;
            }

            return new FilePriorityCounts(candidates.Length, priority, nonPriority);
        }

        public IReadOnlyList<BackupBatchItemResult> LaunchBackupsInParallel(IReadOnlyList<Save> saves)
        {
            var orderedSaves = (saves ?? Array.Empty<Save>())
                .Where(s => s is not null)
                .Distinct()
                .ToList();

            if (orderedSaves.Count == 0)
                return Array.Empty<BackupBatchItemResult>();

            if (!HasPriorityExtensionsConfigured() || orderedSaves.Count == 1)
            {
                return ExecutePhaseInParallel(orderedSaves, save => LaunchBackupType(save));
            }

            var workloadBySave = orderedSaves.ToDictionary(s => s, s => GetFilePriorityCounts(s));
            var resultBySave = new Dictionary<Save, int>();

            var priorityPhaseSaves = orderedSaves
                .Where(s => workloadBySave[s].PriorityFiles > 0)
                .ToList();

            var priorityPhaseResults = ExecutePhaseInParallel(
                priorityPhaseSaves,
                save => LaunchBackupType(save, FileSelectionMode.PriorityOnly));

            foreach (var result in priorityPhaseResults)
            {
                resultBySave[result.Save] = result.ResultCode;
            }

            if (priorityPhaseResults.Any(r => !IsValidBackupResult(r.ResultCode)))
            {
                return orderedSaves
                    .Where(resultBySave.ContainsKey)
                    .Select(s => new BackupBatchItemResult(s, resultBySave[s]))
                    .ToList();
            }

            var nonPriorityPhaseResults = ExecutePhaseInParallel(
                orderedSaves,
                save => LaunchBackupType(
                    save,
                    FileSelectionMode.NonPriorityOnly,
                    allowResumeFromCompletedState: workloadBySave[save].PriorityFiles > 0));

            foreach (var result in nonPriorityPhaseResults)
            {
                resultBySave[result.Save] = result.ResultCode;
            }

            return orderedSaves
                .Where(resultBySave.ContainsKey)
                .Select(s => new BackupBatchItemResult(s, resultBySave[s]))
                .ToList();
        }

        private static IReadOnlyList<BackupBatchItemResult> ExecutePhaseInParallel(
            IReadOnlyList<Save> saves,
            Func<Save, int> phaseExecutor)
        {
            if (saves.Count == 0)
                return Array.Empty<BackupBatchItemResult>();

            Task<BackupBatchItemResult>[] tasks = saves
                .Select(save => Task.Run(() =>
                {
                    try
                    {
                        return new BackupBatchItemResult(save, phaseExecutor(save));
                    }
                    catch
                    {
                        return new BackupBatchItemResult(save, 216);
                    }
                }))
                .ToArray();

            Task.WaitAll(tasks);

            var resultBySave = tasks
                .Select(t => t.Result)
                .ToDictionary(r => r.Save, r => r.ResultCode);

            return saves
                .Where(resultBySave.ContainsKey)
                .Select(save => new BackupBatchItemResult(save, resultBySave[save]))
                .ToList();
        }

        private static bool IsValidBackupResult(int resultCode)
        {
            return resultCode == 104 || resultCode == 105 || resultCode == 216;
        }

        private void BackupAllSaves()
        {
            var saves = _viewModel.model.saves.ToList();
            if (saves.Count == 0) return;

            var results = LaunchBackupsInParallel(saves);
            foreach (var result in results)
            {
                _viewModel.view.DisplayMessage(result.ResultCode);

                if (IsValidBackupResult(result.ResultCode))
                {
                    _viewModel.model.FinishBackup(result.Save);
                }

                if (_viewModel.model.PeekStopReason() == BackupStopReason.BusinessSoftwareDetected)
                {
                    break;
                }

                _viewModel.view.DisplayMessage(4);
            }
        }

        private void BackupSingleSave(int userChoice)
        {
            int indexsave = userChoice - 2;
            Save selectedSave = _viewModel.model.saves[indexsave];

            int backupResult = LaunchBackupType(selectedSave);
            _viewModel.view.DisplayMessage(backupResult);

            if (backupResult == 104 || backupResult == 105 || backupResult == 216)
            {
                _viewModel.model.FinishBackup(selectedSave);
            }
        }

        private int ExecuteBackupStrategy(
            Save _save,
            DirectoryInfo _dir,
            FileSelectionMode selectionMode,
            bool allowResumeFromCompletedState)
        {
            if (!TryGetCandidateFiles(_save, _dir, out FileInfo[] candidates, out int code))
                return code;

            var priorityExtensions = GetConfiguredPriorityExtensions();
            FileInfo[] files = ApplySelectionToFiles(candidates, selectionMode, priorityExtensions);
            long totalSize = files.Sum(f => f.Length);

            if (files.Length == 0)
                return CompleteBackupWithoutFiles(_save);

            return DoBackup(_save, files, totalSize, allowResumeFromCompletedState);
        }

        private bool TryGetCandidateFiles(Save save, DirectoryInfo dir, out FileInfo[] files, out int code)
        {
            files = Array.Empty<FileInfo>();
            code = 0;

            switch (save.backupType)
            {
                case BackupType.FULL:
                    files = dir.GetFiles("*.*", SearchOption.AllDirectories);
                    return true;

                case BackupType.DIFFERENTIAL:
                    string? fullBackupDir = GetFullBackupDir(save);
                    if (string.IsNullOrWhiteSpace(fullBackupDir))
                    {
                        files = dir.GetFiles("*.*", SearchOption.AllDirectories);
                        return true;
                    }

                    files = BuildDifferentialCandidateFiles(save, dir, fullBackupDir);
                    return true;

                default:
                    code = 208;
                    return false;
            }
        }

        private FileInfo[] BuildDifferentialCandidateFiles(Save save, DirectoryInfo sourceDir, string fullBackupDir)
        {
            FileInfo[] srcFiles = sourceDir.GetFiles("*.*", SearchOption.AllDirectories);
            List<FileInfo> filesToCopy = new List<FileInfo>();

            foreach (FileInfo file in srcFiles)
            {
                string currFullBackPath = Path.Combine(fullBackupDir, Path.GetRelativePath(save.src, file.FullName));

                if (!File.Exists(currFullBackPath) || !IsSameFile(currFullBackPath, file.FullName))
                {
                    filesToCopy.Add(file);
                }
            }

            return filesToCopy.ToArray();
        }

        private int CompleteBackupWithoutFiles(Save save)
        {
            save.lastBackupDate = DateTime.Now.ToString("yyyy/MM/dd_HH:mm:ss");
            _viewModel.model.AddLogInJSONFile();

            if (_viewModel.guiView is null)
            {
                _viewModel.view.DisplayMessage(3);
                _viewModel.view.DisplayBackupRecap(save.name, 0);
            }

            _viewModel.guiView?.OnBackupComplete(save.name, 0);
            return 105;
        }

        private string? GetFullBackupDir(Save _save)
        {
            try
            {
                DirectoryInfo[] dirs = new DirectoryInfo(_save.dst).GetDirectories();

                var candidates = dirs
                    .Where(d => d.Name.StartsWith(_save.name + "_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var latest = candidates.OrderByDescending(d => d.CreationTimeUtc).FirstOrDefault();
                return latest?.FullName;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryReadEasySaveCryptoHeader(string filePath, out bool isEncrypted, out string? plaintextSha256Hex)
        {
            isEncrypted = false;
            plaintextSha256Hex = null;

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buf = new byte[256];
                int read = fs.Read(buf, 0, buf.Length);
                if (read <= 0) return false;

                int nl1 = Array.IndexOf(buf, (byte)'\n', 0, read);
                if (nl1 <= 0) return false;

                string line1 = Encoding.ASCII.GetString(buf, 0, nl1).TrimEnd('\r');
                if (!string.Equals(line1, EasySaveCryptoMagicV1, StringComparison.Ordinal) &&
                    !string.Equals(line1, EasySaveCryptoMagicV2, StringComparison.Ordinal))
                {
                    return false;
                }

                isEncrypted = true;

                if (string.Equals(line1, EasySaveCryptoMagicV2, StringComparison.Ordinal))
                {
                    int start2 = nl1 + 1;
                    int nl2 = Array.IndexOf(buf, (byte)'\n', start2, read - start2);
                    if (nl2 > start2)
                    {
                        string line2 = Encoding.ASCII.GetString(buf, start2, nl2 - start2).TrimEnd('\r').Trim();
                        if (line2.Length == 64 && line2.All(Uri.IsHexDigit))
                        {
                            plaintextSha256Hex = line2.ToLowerInvariant();
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ComputeSha256Hex(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hash = sha.ComputeHash(fs);

            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private bool IsSameFile(string path1, string path2)
        {
            try
            {
                if (TryReadEasySaveCryptoHeader(path1, out bool isEncrypted, out string? expectedHash) &&
                    isEncrypted &&
                    !string.IsNullOrWhiteSpace(expectedHash))
                {
                    string actual = ComputeSha256Hex(path2);
                    return string.Equals(expectedHash, actual, StringComparison.OrdinalIgnoreCase);
                }

                byte[] file1 = File.ReadAllBytes(path1);
                byte[] file2 = File.ReadAllBytes(path2);

                if (file1.Length != file2.Length) return false;

                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i]) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? TryGetExistingBackupRootFromState(Save save, bool allowCompletedProgress)
        {
            try
            {
                if (save.state is null) return null;
                if (save.state.progress <= 0) return null;
                if (!allowCompletedProgress && save.state.progress >= 100) return null;

                string currentDest = save.state.currentPathDest ?? "";
                if (string.IsNullOrWhiteSpace(currentDest)) return null;

                string dstBase = Path.GetFullPath(save.dst)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                string dir = Directory.Exists(currentDest)
                    ? currentDest
                    : (Path.GetDirectoryName(currentDest) ?? "");

                if (string.IsNullOrWhiteSpace(dir)) return null;

                var di = new DirectoryInfo(dir);
                for (int i = 0; i < 20 && di is not null; i++)
                {
                    var parent = di.Parent;
                    if (parent is null) break;

                    string parentFull = parent.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (string.Equals(parentFull, dstBase, StringComparison.OrdinalIgnoreCase))
                    {
                        return di.FullName;
                    }

                    di = parent;
                }
            }
            catch
            {
                // Intentionally ignored.
            }

            return null;
        }

        private int DoBackup(
            Save _save,
            FileInfo[] _files,
            long _totalSize,
            bool allowResumeFromCompletedState)
        {
            DateTime startTime = DateTime.Now;
            string? resumeRoot = TryGetExistingBackupRootFromState(_save, allowResumeFromCompletedState);
            string dst;

            if (!string.IsNullOrWhiteSpace(resumeRoot) && Directory.Exists(resumeRoot))
            {
                dst = resumeRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            }
            else
            {
                string backupDirName = _save.name + "_" + startTime.ToString("yyyy-MM-dd_HH-mm-ss");
                dst = Path.Combine(_save.dst, backupDirName) + Path.DirectorySeparatorChar;
            }

            _save.state = new State(_files.Length, _totalSize, _save.src, dst);
            _save.lastBackupDate = startTime.ToString("yyyy/MM/dd_HH:mm:ss");

            try
            {
                Directory.CreateDirectory(dst);
            }
            catch
            {
                return 210;
            }

            if (_viewModel.guiView is null)
            {
                try { Console.Clear(); } catch { }
            }

            var activeSw = Stopwatch.StartNew();

            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst, activeSw, out int encryptedCount);

            activeSw.Stop();
            double transferTime = activeSw.Elapsed.TotalMilliseconds;

            _viewModel.model.AddLogInJSONFile();

            bool stopped = _viewModel.model.IsStopRequested() || _viewModel.model.IsStopRequested(_save.name);

            if (_viewModel.guiView is null)
            {
                _viewModel.view.DisplayMessage(3);
            }

            foreach (string failedFile in failedFiles)
            {
                if (_viewModel.guiView is null)
                {
                    _viewModel.view.DisplayFiledError(failedFile);
                }
                _viewModel.guiView?.OnFileError(failedFile);
            }

            if (_viewModel.guiView is null)
            {
                _viewModel.view.DisplayBackupRecap(_save.name, transferTime);
            }

            if (!stopped)
            {
                _viewModel.guiView?.OnBackupComplete(_save.name, transferTime);
                _viewModel.guiView?.OnEncryptionSummary(_save.name, encryptedCount);
            }

            return stopped ? 216 : (failedFiles.Count == 0 ? 104 : 216);
        }

        private static string FormatEta(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms <= 0) return "Calcul...";

            var ts = TimeSpan.FromMilliseconds(ms);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        private static string NormalizeExtension(string ext)
        {
            ext = (ext ?? string.Empty).Trim();
            if (ext.Length == 0) return string.Empty;
            if (!ext.StartsWith(".")) ext = "." + ext;
            return ext.ToLowerInvariant();
        }

        private HashSet<string> GetConfiguredPriorityExtensions()
        {
            var cfg = _viewModel.model.GetConfig();

            return (cfg.PriorityExtensions ?? new List<string>())
                .Select(NormalizeExtension)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPriorityExtension(string extension, HashSet<string> priorityExtensions)
        {
            if (priorityExtensions is null || priorityExtensions.Count == 0)
                return false;

            string normalized = NormalizeExtension(extension);
            return !string.IsNullOrWhiteSpace(normalized) && priorityExtensions.Contains(normalized);
        }

        private static FileInfo[] ApplySelectionToFiles(
            FileInfo[] files,
            FileSelectionMode selectionMode,
            HashSet<string> priorityExtensions)
        {
            if (files.Length == 0)
                return files;

            return selectionMode switch
            {
                FileSelectionMode.PriorityOnly => files
                    .Where(f => IsPriorityExtension(Path.GetExtension(f.FullName), priorityExtensions))
                    .ToArray(),
                FileSelectionMode.NonPriorityOnly => files
                    .Where(f => !IsPriorityExtension(Path.GetExtension(f.FullName), priorityExtensions))
                    .ToArray(),
                _ => priorityExtensions.Count == 0
                    ? files
                    : files
                        .OrderByDescending(f => IsPriorityExtension(Path.GetExtension(f.FullName), priorityExtensions))
                        .ThenBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
                        .ToArray()
            };
        }

        private bool ShouldForceEncryptAllExtensions(FileInfo[] files)
        {
            var cfg = _viewModel.model.GetConfig();
            if (!cfg.EnableEncryption) return false;

            var normalizedConfiguredExtensions = (cfg.EncryptionExtensions ?? new List<string>())
                .Select(NormalizeExtension)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedConfiguredExtensions.Count == 0) return false;

            foreach (var file in files)
            {
                string fileExt = NormalizeExtension(Path.GetExtension(file.FullName));
                if (string.IsNullOrWhiteSpace(fileExt)) continue;

                if (normalizedConfiguredExtensions.Any(e => string.Equals(e, fileExt, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }

        private List<string> CopyFiles(
            Save _save,
            FileInfo[] _files,
            long _totalSize,
            string _dst,
            Stopwatch activeSw,
            out int encryptedCount)
        {
            long leftSize = _totalSize;
            int totalFile = _files.Length;

            List<string> failedFiles = new List<string>();
            encryptedCount = 0;

            double emaSpeedBytesPerMs = 0.0;
            const double alpha = 0.20;
            long bytesCopiedSuccess = 0;
            bool businessPauseActive = false;
            string pausedBusinessProcess = "";
            bool forceEncryptAllExtensions = ShouldForceEncryptAllExtensions(_files);

            if (forceEncryptAllExtensions)
            {
                _viewModel.guiView?.ShowMessage(ResourceManager.GetString("UiEncryptionFallbackAllFiles"));
            }

            for (int i = 0; i < _files.Length; i++)
            {
                bool businessRunning = _viewModel.model.TryGetRunningBusinessSoftware(out string runningBusinessProcess);
                bool globalStopRequested = _viewModel.model.IsStopRequested();
                bool saveStopRequested = _viewModel.model.IsStopRequested(_save.name);
                bool manualPauseRequested =
                    _viewModel.model.IsPauseRequested() ||
                    _viewModel.model.IsPauseRequested(_save.name);

                if ((manualPauseRequested || businessRunning) && !globalStopRequested && !saveStopRequested)
                {
                    activeSw.Stop();

                    if (businessRunning && !businessPauseActive)
                    {
                        string spec = string.IsNullOrWhiteSpace(runningBusinessProcess)
                            ? _viewModel.model.GetBusinessSoftwareSpec()
                            : runningBusinessProcess;
                        if (string.IsNullOrWhiteSpace(spec)) spec = "business software";
                        string pausedText = string.Format(ResourceManager.GetString("UiPausedByBusinessDetected"), spec);
                        _viewModel.guiView?.ShowMessage(pausedText);
                        pausedBusinessProcess = spec;
                        businessPauseActive = true;
                    }

                    while (true)
                    {
                        bool stillGlobalStop = _viewModel.model.IsStopRequested();
                        bool stillSaveStop = _viewModel.model.IsStopRequested(_save.name);
                        if (stillGlobalStop || stillSaveStop) break;

                        bool stillManualPause =
                            _viewModel.model.IsPauseRequested() ||
                            _viewModel.model.IsPauseRequested(_save.name);
                        bool stillBusinessPause = _viewModel.model.TryGetRunningBusinessSoftware(out _);
                        if (!stillManualPause && !stillBusinessPause) break;
                        Thread.Sleep(200);
                    }

                    if (!_viewModel.model.IsStopRequested() && !_viewModel.model.IsStopRequested(_save.name))
                    {
                        if (businessPauseActive && !_viewModel.model.TryGetRunningBusinessSoftware(out _))
                        {
                            string spec = string.IsNullOrWhiteSpace(pausedBusinessProcess)
                                ? _viewModel.model.GetBusinessSoftwareSpec()
                                : pausedBusinessProcess;
                            if (string.IsNullOrWhiteSpace(spec)) spec = "business software";
                            string resumedText = string.Format(ResourceManager.GetString("UiResumedAfterBusiness"), spec);
                            _viewModel.guiView?.ShowMessage(resumedText);
                            businessPauseActive = false;
                            pausedBusinessProcess = "";
                        }

                        activeSw.Start();
                    }
                }

                if (_viewModel.model.IsStopRequested() || _viewModel.model.IsStopRequested(_save.name))
                {
                    var reason = _viewModel.model.PeekStopReason();
                    var detail = _viewModel.model.PeekStopDetail();
                    if (reason == BackupStopReason.None)
                    {
                        reason = _viewModel.model.PeekStopReason(_save.name);
                        detail = _viewModel.model.PeekStopDetail(_save.name);
                    }

                    if (reason == BackupStopReason.None) reason = BackupStopReason.UserRequested;

                    _viewModel.model.WriteBackupStopLog(_save.name, reason, _save.state?.currentPathSrc);

                    if (reason == BackupStopReason.UserRequested &&
                        string.Equals(detail, "cleanup", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (Directory.Exists(_dst))
                                Directory.Delete(_dst, recursive: true);
                        }
                        catch { }
                    }

                    break;
                }

                int pourcent = ((i + 1) * 100) / totalFile;
                long curSize = _files[i].Length;
                leftSize -= curSize;

                DateTime fileStartUtc = DateTime.UtcNow;

                bool ok = _viewModel.model.TryCopyFile(
                    _save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent,
                    out string? error, out EncryptionAction encryptionAction, forceEncryptAllExtensions);

                double fileMs = (DateTime.UtcNow - fileStartUtc).TotalMilliseconds;
                if (fileMs < 1) fileMs = 1;

                string eta = "Calcul...";

                if (ok)
                {
                    if (encryptionAction == EncryptionAction.Encrypted) encryptedCount++;

                    bytesCopiedSuccess += curSize;

                    double speedThisFile = curSize / fileMs;
                    emaSpeedBytesPerMs = (emaSpeedBytesPerMs <= 0.0)
                        ? speedThisFile
                        : (alpha * speedThisFile + (1.0 - alpha) * emaSpeedBytesPerMs);

                    Thread.Sleep((int)(curSize / 1000000));
                }
                else
                {
                    string detail = string.IsNullOrWhiteSpace(error) ? "Copy error." : error;

                    _viewModel.guiView?.OnFileError($"{_files[i].FullName}: {detail}");
                    failedFiles.Add($"{_files[i].FullName}: {detail}");

                    if (_viewModel.model.IsStopRequested() || _viewModel.model.IsStopRequested(_save.name))
                    {
                        var reason = _viewModel.model.PeekStopReason();
                        var stopDetail = _viewModel.model.PeekStopDetail();
                        if (reason == BackupStopReason.None)
                        {
                            reason = _viewModel.model.PeekStopReason(_save.name);
                            stopDetail = _viewModel.model.PeekStopDetail(_save.name);
                        }

                        if (reason == BackupStopReason.None) reason = BackupStopReason.UserRequested;

                        _viewModel.model.WriteBackupStopLog(_save.name, reason, _save.state?.currentPathSrc);

                        if (reason == BackupStopReason.UserRequested &&
                            string.Equals(stopDetail, "cleanup", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (Directory.Exists(_dst))
                                    Directory.Delete(_dst, recursive: true);
                            }
                            catch { }
                        }

                        break;
                    }
                }

                if (emaSpeedBytesPerMs > 0.0 && i >= 2 && bytesCopiedSuccess > 0 && activeSw.ElapsedMilliseconds > 500)
                {
                    double remainingMs = leftSize / emaSpeedBytesPerMs;
                    eta = FormatEta(remainingMs);
                }

                if (_viewModel.guiView is null)
                {
                    _viewModel.view.DisplayCurrentState(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);
                }

                _viewModel.guiView?.OnProgressUpdate($"{_save.name} | ETA {eta}", totalFile - i - 1, leftSize, curSize, pourcent);

            }

            return failedFiles;
        }
    }
}
