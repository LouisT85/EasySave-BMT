using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using easySave_BMT.ViewModel_;

namespace easySave_BMT.ViewModel_.Backup
{
    /// <summary>
    /// Executes full and differential backups and reports progress to console and GUI observers.
    /// </summary>
    public class BackupLauncher
    {
        private const string EasySaveCryptoMagicV1 = "EASYSAVECRYPT1";
        private const string EasySaveCryptoMagicV2 = "EASYSAVECRYPT2";

        private readonly Model _model;
        private readonly View _view;
        private readonly Func<IProgressObserverGUI?> _guiAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupLauncher"/> class.
        /// </summary>
        /// <param name="model">The domain model facade.</param>
        /// <param name="view">The console view adapter.</param>
        /// <param name="guiAccessor">Returns the current GUI observer when available.</param>
        public BackupLauncher(Model model, View view, Func<IProgressObserverGUI?> guiAccessor)
        {
            _model = model;
            _view = view;
            _guiAccessor = guiAccessor;
        }

        /// <summary>
        /// Prompts the user for backup execution in console mode and runs the selected jobs.
        /// </summary>
        public void LaunchBackupsave()
        {
            if (_model.saves.Count > 0)
            {
                int userChoice = _view.LaunchBackupChoice();

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
                _view.DisplayMessage(1);
            }
            else
            {
                _view.DisplayMessage(204);
            }
        }

        /// <summary>
        /// Launches one save using its configured backup strategy.
        /// </summary>
        /// <param name="_save">The save definition to run.</param>
        /// <returns>A status code representing the execution result.</returns>
        public int LaunchBackupType(Save _save)
        {
            // Business software check: if detected, do not start the backup.
            if (_viewModel.model.IsBusinessSoftwareRunning())
            {
                _viewModel.model.RequestStop(BackupStopReason.BusinessSoftwareDetected, _viewModel.model.GetBusinessSoftwareSpec());
                _viewModel.model.WriteBackupStopLog(_save.name, BackupStopReason.BusinessSoftwareDetected);
                return 216;
            }

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
                // If path normalization fails, proceed and let the copy layer report errors.
            }

            var activeState = new State(0, 0, _save.src, _save.dst);
            _save.state = activeState;
            _model.UpdateSaveState(_save);

            return ExecuteBackupStrategy(_save, dir);
        }

        private void BackupAllSaves()
        {
            foreach (Save save in _model.saves)
            {
                int result = LaunchBackupType(save);
                _view.DisplayMessage(result);

                if (result == 104 || result == 105 || result == 216)
                {
                    _model.FinishBackup(save);
                }

                _view.DisplayMessage(4);
            }
        }

        private void BackupSingleSave(int userChoice)
        {
            int indexsave = userChoice - 2;
            Save selectedSave = _model.saves[indexsave];
            int backupResult = LaunchBackupType(selectedSave);
            _view.DisplayMessage(backupResult);

            if (backupResult == 104 || backupResult == 105 || backupResult == 216)
            {
                _model.FinishBackup(selectedSave);
            }
        }

        private int ExecuteBackupStrategy(Save _save, DirectoryInfo _dir)
        {
            switch (_save.backupType)
            {
                case BackupType.DIFFERENTIAL:
                    string? fullBackupDir = GetFullBackupDir(_save);
                    if (fullBackupDir != null)
                    {
                        return DifferentialBackupSetup(_save, _dir, fullBackupDir);
                    }
                    return FullBackupSetup(_save, _dir);

                case BackupType.FULL:
                    return FullBackupSetup(_save, _dir);

                default:
                    return 208;
            }
        }

        private string? GetFullBackupDir(Save _save)
        {
            try
            {
                DirectoryInfo[] dirs = new DirectoryInfo(_save.dst).GetDirectories();

                // Pick the most recent backup folder for this save name.
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

        private int FullBackupSetup(Save _save, DirectoryInfo _dir)
        {
            long totalSize = 0;
            FileInfo[] files = _dir.GetFiles("*.*", SearchOption.AllDirectories);

            foreach (FileInfo file in files)
            {
                totalSize += file.Length;
            }
            return DoBackup(_save, files, totalSize);
        }

        private int DifferentialBackupSetup(Save _save, DirectoryInfo _dir, string _fullBackupDir)
        {
            long totalSize = 0;
            FileInfo[] srcFiles = _dir.GetFiles("*.*", SearchOption.AllDirectories);
            List<FileInfo> filesToCopy = new List<FileInfo>();

            foreach (FileInfo file in srcFiles)
            {
                string currFullBackPath = Path.Combine(_fullBackupDir, Path.GetRelativePath(_save.src, file.FullName));

                if (!File.Exists(currFullBackPath) || !IsSameFile(currFullBackPath, file.FullName))
                {
                    totalSize += file.Length;
                    filesToCopy.Add(file);
                }
            }

            if (filesToCopy.Count == 0)
            {
                _save.lastBackupDate = DateTime.Now.ToString("yyyy/MM/dd_HH:mm:ss");
                _model.AddLogInJSONFile();

                // Notification console (only when running in console mode)
                if (_guiAccessor() is null)
                {
                    _view.DisplayMessage(3);
                    _view.DisplayBackupRecap(_save.name, 0);
                }

                _guiAccessor()?.OnBackupComplete(_save.name, 0);
                return 105;
            }
            return DoBackup(_save, filesToCopy.ToArray(), totalSize);
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
                        if (line2.Length == 64 && line2.All(ch => Uri.IsHexDigit(ch)))
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

        private static bool HasEasySaveCryptoHeader(string filePath)
        {
            return TryReadEasySaveCryptoHeader(filePath, out bool isEncrypted, out _) && isEncrypted;
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
                // If the reference file is EasySave-encrypted (v2), compare the stored plaintext hash to avoid decrypting.
                if (TryReadEasySaveCryptoHeader(path1, out bool isEncrypted, out string? expectedHash) &&
                    isEncrypted &&
                    !string.IsNullOrWhiteSpace(expectedHash))
                {
                    string actual = ComputeSha256Hex(path2);
                    return string.Equals(expectedHash, actual, StringComparison.OrdinalIgnoreCase);
                }

                // Fallback: byte-by-byte equality (works for non-encrypted backups and already-encrypted sources).
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

        private int DoBackup(Save _save, FileInfo[] _files, long _totalSize)
        {
            DateTime startTime = DateTime.Now;
            string backupDirName = _save.name + "_" + startTime.ToString("yyyy-MM-dd_HH-mm-ss");
            string dst = Path.Combine(_save.dst, backupDirName) + Path.DirectorySeparatorChar;

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

            // In Avalonia (WinExe), there is no console attached; Console.Clear/SetCursorPosition can throw.
            if (_guiAccessor() is null)
            {
                try { Console.Clear(); } catch { /* ignore */ }
            }

            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst, out int encryptedCount, out int skippedEncryptedCount);
            DateTime endTime = DateTime.Now;
            TimeSpan saveTime = endTime - startTime;
            double transferTime = saveTime.TotalMilliseconds;

                _model.AddLogInJSONFile();

                // Notifications console (only when running in console mode)
                if (_guiAccessor() is null)
                {
                    _view.DisplayMessage(3);
                }

                foreach (string failedFile in failedFiles)
                {
                    if (_guiAccessor() is null)
                    {
                        _view.DisplayFiledError(failedFile);
                    }
                    _guiAccessor()?.OnFileError(failedFile);
                }

                if (_guiAccessor() is null)
                {
                    _view.DisplayBackupRecap(_save.name, transferTime);
                }
                _guiAccessor()?.OnBackupComplete(_save.name, transferTime);
                _guiAccessor()?.OnEncryptionSummary(_save.name, encryptedCount, skippedEncryptedCount);

            return failedFiles.Count == 0 ? 104 : 216;
        }

        private List<string> CopyFiles(Save _save, FileInfo[] _files, long _totalSize, string _dst, out int encryptedCount, out int skippedAlreadyEncryptedCount)
        {
            long leftSize = _totalSize;
            int totalFile = _files.Length;
            List<string> failedFiles = new List<string>();
            encryptedCount = 0;
            skippedAlreadyEncryptedCount = 0;

            for (int i = 0; i < _files.Length; i++)
            {
                // Manual stop requested: stop between files (finish current file is already done).
                if (_viewModel.model.IsStopRequested())
                {
                    var reason = _viewModel.model.PeekStopReason();
                    if (reason == BackupStopReason.None) reason = BackupStopReason.UserRequested;

                    // Log the last completed/in-progress file (state was set before the copy).
                    _viewModel.model.WriteBackupStopLog(_save.name, reason, _save.state?.currentPathSrc);
                    break;
                }

                int pourcent = ((i + 1) * 100) / totalFile;
                long curSize = _files[i].Length;
                leftSize -= curSize;

                if (_model.TryCopyFile(_save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent, out string? error, out EncryptionAction encryptionAction))
                {
                    if (encryptionAction == EncryptionAction.Encrypted) encryptedCount++;
                    else if (encryptionAction == EncryptionAction.SkippedAlreadyEncrypted) skippedAlreadyEncryptedCount++;

                    Thread.Sleep((int)(curSize / 1000000));
                    // Mise à jour de la progression en console (only when running in console mode)
                    if (_guiAccessor() is null)
                    {
                        _view.DisplayCurrentState(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);
                    }

                    // Mise à jour de la progression en GUI (barre de progression / texte)
                    _guiAccessor()?.OnProgressUpdate(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);
                }
                else
                {
                    // Still publish progress so the UI doesn't look stuck on failures.
                    _guiAccessor()?.OnProgressUpdate(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);

                    string detail = string.IsNullOrWhiteSpace(error) ? "Erreur de copie." : error;
                    _guiAccessor()?.OnFileError($"{_files[i].FullName}: {detail}");
                    failedFiles.Add($"{_files[i].FullName}: {detail}");
                }

                // Business software detection: finish this file then stop before the next one.
                if (_viewModel.model.IsBusinessSoftwareRunning())
                {
                    _viewModel.model.RequestStop(BackupStopReason.BusinessSoftwareDetected, _viewModel.model.GetBusinessSoftwareSpec());
                    _viewModel.model.WriteBackupStopLog(_save.name, BackupStopReason.BusinessSoftwareDetected, _files[i].FullName);
                    break;
                }
            }
            return failedFiles;
        }
    }
}

