using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Diagnostics;
using easySave_BMT.ViewModel_;
using EasyLog;
using EasyLog.Models;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using easySave_BMT.Resources_;
namespace easySave_BMT.Model_
{
    /// <summary>
    /// Core logic class of the application. It manages the list of backup jobs, 
    /// file operations, logging orchestration, and configuration persistence.
    /// </summary>
    public class Model
    {
        private EasyLogger xmlLogger;
        private EasyLogger jsonLogger;
        private Config config;
        private string backupsaveSavePath = "./BackupSave.json";
        private readonly object _backupSaveFileLock = new();

        private volatile bool _stopRequested;
        private volatile BackupStopReason _stopReason = BackupStopReason.None;
        private string? _stopDetail;
        private readonly object _stopLock = new();
        private volatile bool _pauseRequested;

        /// <summary>List of all configured backup jobs.</summary>
        public List<Save> saves { get; private set; }

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Initializes the Model, loads user configuration, sets up the logger, 
        /// and initializes the state file paths.
        /// </summary>
        public Model()
        {
            this.saves = new List<Save>();
            config = Config.Load();

            // Initialize global resources based on config
            ResourceManager.SetLanguage(config.Language);
            RealTimeState.SetFilePath(config.StateFilePath);
            Directory.CreateDirectory(config.LogDirectory);
            // Always produce both XML and JSON logs in parallel.
            xmlLogger = new EasyLogger(config.LogDirectory, EasyLogger.LogFormat.XML);
            jsonLogger = new EasyLogger(config.LogDirectory, EasyLogger.LogFormat.JSON);

            Console.WriteLine($"Logs directory: {config.LogDirectory}");
            Console.WriteLine($"State file: {config.StateFilePath}");
        }

        /// <summary>
        /// Adds a new backup job to the list and persists the changes.
        /// </summary>
        /// <returns>Status code: 101 for success, 201 for failure.</returns>
        public int AddSave(string name, string src, string dst, BackupType backupType)
        {
            try
            {
                name = (name ?? string.Empty).Trim();
                src = (src ?? string.Empty).Trim();
                dst = (dst ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    return 215; // EnterValidName
                }

                // Reject duplicate names (case-insensitive) to avoid ambiguity in GUI multi-select / runner.
                if (this.saves.Any(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    return 214; // NameTaken
                }

                // Validate source/destination at creation time (user request).
                if (!Directory.Exists(src))
                {
                    return 211; // DirectoryNotExist
                }

                if (!Directory.Exists(dst))
                {
                    return 213; // DestinationNotExist
                }

                // Prevent destination being inside source (can cause recursion / unexpected behavior).
                try
                {
                    string srcFull = Path.GetFullPath(src).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    string dstFull = Path.GetFullPath(dst).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                    if (string.Equals(srcFull, dstFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return 212; // ChooseDifferentPath
                    }

                    if (dstFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return 217; // DestinationInsideSource
                    }
                }
                catch
                {
                    // If normalization fails, let the backup layer handle it later.
                }

                this.saves.Add(new Save(name, src, dst, backupType));
                AddLogInJSONFile();

                // Create initial inactive state in the state file
                var inactiveState = State.CreateInactiveState(name);
                RealTimeState.SaveStates(new List<RealTimeState> { inactiveState });

                return 101;
            }
            catch
            {
                return 201;
            }
        }

        /// <summary>
        /// Removes a backup job from the list at the specified index.
        /// </summary>
        /// <returns>Status code: 103 for success, 203 for failure.</returns>
        public int RemoveSave(int index)
        {
            try
            {
                string removedName = this.saves[index].name;
                this.saves.RemoveAt(index);
                AddLogInJSONFile();

                // Clean up the real-time state file
                RealTimeState.RemoveState(removedName);

                return 103;
            }
            catch
            {
                return 203;
            }
        }

        /// <summary>
        /// Wrapper method to trigger save data loading.
        /// </summary>
        public int CreateLogs()
        {
            return ReloadSavesFromFile();
        }

        /// <summary>
        /// Loads the list of backup jobs from the BackupSave.json file.
        /// </summary>
        /// <returns>Status code: 100 for success, 200 for error.</returns>
        public int ReloadSavesFromFile()
        {
            if (File.Exists(backupsaveSavePath))
            {
                try
                {
                    string jsonContent;
                    lock (_backupSaveFileLock)
                    {
                        jsonContent = File.ReadAllText(this.backupsaveSavePath);
                    }
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        this.saves = JsonSerializer.Deserialize<List<Save>>(jsonContent) ?? new List<Save>();
                    }
                    else
                    {
                        this.saves = new List<Save>();
                    }

                    return 100;
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON Error: {jsonEx.Message}");
                    return 200;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading saves: {ex.Message}");
                    return 200;
                }
            }
            else
            {
                this.saves = new List<Save>();
                return 100;
            }
        }

        /// <summary>
        /// Serializes the current list of backup jobs to the JSON save file.
        /// </summary>
        public void AddLogInJSONFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(this.saves, this.jsonOptions);
                lock (_backupSaveFileLock)
                {
                    File.WriteAllText(this.backupsaveSavePath, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes the current execution states of all jobs with the real-time state file.
        /// </summary>
        public void SaveStates()
        {
            try
            {
                List<RealTimeState> statesToSave = new List<RealTimeState>();

                foreach (var save in this.saves)
                {
                    RealTimeState state;
                    if (save.state != null)
                    {
                        state = save.state.ToRealTimeState(save.name);
                    }
                    else
                    {
                        state = State.CreateInactiveState(save.name);
                    }
                    statesToSave.Add(state);
                }

                RealTimeState.SaveStates(statesToSave);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving states: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the real-time file for a single backup job.
        /// </summary>
        /// <param name="save">The backup job to update.</param>
        public void UpdateSaveState(Save save)
        {
            try
            {
                RealTimeState state;
                if (save.state != null)
                {
                    state = save.state.ToRealTimeState(save.name);
                }
                else
                {
                    state = State.CreateEndState(save.name);
                }

                RealTimeState.SaveStates(new List<RealTimeState> { state });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating state: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs the physical copy of a file, updates progress state, and logs the operation.
        /// </summary>
        /// <returns>True if the file was copied successfully, otherwise false.</returns>
        public bool CopyFile(
            Save save,
            FileInfo currentFile,
            long curSize,
            string dst,
            long leftSize,
            int totalFile,
            int fileIndex,
            int pourcent)
        {
            // Legacy signature kept for console/older call sites.
            return TryCopyFile(save, currentFile, curSize, dst, leftSize, totalFile, fileIndex, pourcent, out _);
        }

        /// <summary>
        /// Performs the physical copy of a file, updates progress state, and logs the operation.
        /// Provides an error message when the copy fails.
        /// </summary>
        public bool TryCopyFile(
            Save save,
            FileInfo currentFile,
            long curSize,
            string dst,
            long leftSize,
            int totalFile,
            int fileIndex,
            int pourcent,
            out string? error)
        {
            // Legacy signature kept for existing call sites.
            return TryCopyFile(save, currentFile, curSize, dst, leftSize, totalFile, fileIndex, pourcent, out error, out _);
        }

        /// <summary>
        /// Same as <see cref="TryCopyFile(Save, FileInfo, long, string, long, int, int, int, out string?)"/> but also
        /// reports whether encryption was applied or skipped (already encrypted).
        /// </summary>
        public bool TryCopyFile(
            Save save,
            FileInfo currentFile,
            long curSize,
            string dst,
            long leftSize,
            int totalFile,
            int fileIndex,
            int pourcent,
            out string? error,
            out EncryptionAction encryptionAction)
        {
            string curDirPath = currentFile.DirectoryName ?? save.src ?? "";
            string dstDirectory = dst;
            string dstFile = "";

            try
            {
                error = null;
                encryptionAction = EncryptionAction.None;

                // Handle sub-directory structure at the destination (may throw on invalid/unauthorized paths)
                string relativeDir = Path.GetRelativePath(save.src ?? "", curDirPath);
                if (!string.IsNullOrWhiteSpace(relativeDir) && relativeDir != ".")
                {
                    dstDirectory = Path.Combine(dstDirectory, relativeDir);
                }

                // Ensure target directory exists (can throw UnauthorizedAccessException)
                Directory.CreateDirectory(dstDirectory);

                dstFile = Path.Combine(dstDirectory, currentFile.Name);

                // Update dynamic state before starting the copy
                save.state.UpdateState(
                    pourcent,
                    (totalFile - fileIndex),
                    leftSize,
                    currentFile.FullName,
                    dstFile
                );

                UpdateSaveState(save);

                // Notification de progression éventuelle pour la GUI
                // (l'observateur GUI est porté par le ViewModel qui consomme ces états)

                // Decide encryption from the source content (avoid XOR "decrypt" if file is already encrypted).
                bool shouldEncrypt = ShouldEncryptFile(currentFile.FullName);

                // Perform file copy (and compute plaintext hash only if we will encrypt).
                string? plaintextHashHex = null;
                var copySw = Stopwatch.StartNew();
                if (shouldEncrypt)
                {
                    using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                    byte[] buffer = new byte[81920];

                    using (var inFs = new FileStream(currentFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var outFs = new FileStream(dstFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        int read;
                        while ((read = inFs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outFs.Write(buffer, 0, read);
                            hasher.AppendData(buffer, 0, read);
                        }

                        outFs.Flush(true);
                    }

                    plaintextHashHex = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
                }
                else
                {
                    currentFile.CopyTo(dstFile, true);
                }
                copySw.Stop();

                long transferTime = copySw.ElapsedMilliseconds;

                // Optional encryption step (CryptoSoft) depending on user configuration.
                long encryptionTimeMs = 0;
                bool encryptionOk = true;
                string? encryptionError = null;

                if (!shouldEncrypt && config.EnableEncryption)
                {
                    string ext = NormalizeExtension(Path.GetExtension(currentFile.FullName));
                    bool configured = (config.EncryptionExtensions ?? new List<string>())
                        .Any(e => ext == NormalizeExtension(e));
                    if (configured && (HasEasySaveCryptoHeader(currentFile.FullName) || IsLikelyAlreadyEncryptedTextByHeuristic(currentFile.FullName, ext)))
                    {
                        encryptionAction = EncryptionAction.SkippedAlreadyEncrypted;
                    }
                }

                if (shouldEncrypt)
                {
                    encryptionAction = EncryptionAction.Encrypted;
                    encryptionOk = TryEncryptInPlaceWithCryptoSoft(dstFile, plaintextHashHex, out encryptionTimeMs, out encryptionError);
                }

                // Log success
                WriteLogEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = save.name,
                    SourcePath = currentFile.FullName,
                    DestinationPath = dstFile,
                    FileSize = curSize,
                    TransferTimeMs = transferTime,
                    EncryptionTimeMs = encryptionTimeMs
                });

                if (!encryptionOk)
                {
                    // Copy succeeded but encryption failed: surface as a file-level error for the backup.
                    error = encryptionError ?? "Encryption failed.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying file: {ex.Message}");
                error = ex.Message;
                encryptionAction = EncryptionAction.None;

                // Log error (TransferTime set to -1)
                WriteLogEntry(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = save.name,
                    SourcePath = currentFile.FullName,
                    DestinationPath = string.IsNullOrWhiteSpace(dstFile) ? dstDirectory : dstFile,
                    FileSize = curSize,
                    TransferTimeMs = -1,
                    EncryptionTimeMs = 0
                });

                return false;
            }
        }

        /// <summary>
        /// Marks a backup job as finished, resets its progress state, and updates the persistence file.
        /// </summary>
        public void FinishBackup(Save save)
        {
            if (save.state != null)
            {
                save.state.UpdateState(100, 0, 0, "", "");
                UpdateSaveState(save);
            }

            save.state = null;
            UpdateSaveState(save);
        }

        /// <summary>
        /// Gets the current global configuration.
        /// </summary>
        public Config GetConfig()
        {
            return config;
        }

        public void ClearStopRequest()
        {
            lock (_stopLock)
            {
                _stopRequested = false;
                _stopReason = BackupStopReason.None;
                _stopDetail = null;
            }
        }

        public void RequestStop(BackupStopReason reason, string? detail = null)
        {
            lock (_stopLock)
            {
                _stopRequested = true;
                _stopReason = reason;
                _stopDetail = detail;
            }

            // Stop overrides pause.
            _pauseRequested = false;
        }

        public bool IsStopRequested()
        {
            return _stopRequested;
        }

        public void RequestPause()
        {
            _pauseRequested = true;
        }

        public void ClearPauseRequest()
        {
            _pauseRequested = false;
        }

        public bool IsPauseRequested()
        {
            return _pauseRequested;
        }

        public BackupStopReason PeekStopReason()
        {
            lock (_stopLock)
            {
                return _stopRequested ? _stopReason : BackupStopReason.None;
            }
        }

        public string? PeekStopDetail()
        {
            lock (_stopLock)
            {
                return _stopRequested ? _stopDetail : null;
            }
        }

        public bool TryConsumeStopInfo(out BackupStopReason reason, out string? detail)
        {
            lock (_stopLock)
            {
                if (!_stopRequested)
                {
                    reason = BackupStopReason.None;
                    detail = null;
                    return false;
                }

                reason = _stopReason;
                detail = _stopDetail;

                // Consume so subsequent operations start clean.
                _stopRequested = false;
                _stopReason = BackupStopReason.None;
                _stopDetail = null;
                return true;
            }
        }

        /// <summary>
        /// Updates application settings, including language, log directory, and state file path.
        /// </summary>
        public void UpdateConfig(
            string logDir,
            string statePath,
            string language,
            bool? enableEncryption = null,
            List<string>? encryptionExtensions = null,
            string? cryptoSoftPath = null,
            string? businessSoftware = null)
        {
            config.UpdateFromUserInput(logDir, statePath, language, enableEncryption, encryptionExtensions, cryptoSoftPath, businessSoftware);

            if (!string.IsNullOrWhiteSpace(language))
            {
                ResourceManager.SetLanguage(language);
            }

            try
            {
                RealTimeState.SetFilePath(config.StateFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting state file path: {ex.Message}");
            }

            // Refresh logger with new directory and configured format
            try
            {
                Directory.CreateDirectory(config.LogDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating logs directory: {ex.Message}");
            }

            try
            {
                xmlLogger = new EasyLogger(config.LogDirectory, EasyLogger.LogFormat.XML);
                jsonLogger = new EasyLogger(config.LogDirectory, EasyLogger.LogFormat.JSON);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing logger: {ex.Message}");
            }
        }

        private static string NormalizeExtension(string ext)
        {
            ext = (ext ?? string.Empty).Trim();
            if (ext.Length == 0) return string.Empty;
            if (!ext.StartsWith(".")) ext = "." + ext;
            return ext.ToLowerInvariant();
        }

        public string GetBusinessSoftwareSpec()
        {
            return (config.BusinessSoftware ?? "").Trim();
        }

        private static string NormalizeProcessName(string spec)
        {
            spec = (spec ?? "").Trim();
            if (spec.Length == 0) return "";

            try
            {
                // Allow "C:\path\to\calc.exe" as well as "calc" / "calc.exe"
                if (spec.Contains(Path.DirectorySeparatorChar) || spec.Contains(Path.AltDirectorySeparatorChar))
                    spec = Path.GetFileName(spec);
            }
            catch { }

            if (spec.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                spec = spec[..^4];

            return spec.Trim();
        }

        public bool IsBusinessSoftwareRunning()
        {
            string spec = GetBusinessSoftwareSpec();
            if (string.IsNullOrWhiteSpace(spec)) return false;

            string procName = NormalizeProcessName(spec);
            if (string.IsNullOrWhiteSpace(procName)) return false;

            try
            {
                // Fast path on Windows: by name (case-insensitive).
                var procs = Process.GetProcessesByName(procName);
                if (procs is not null && procs.Length > 0) return true;
            }
            catch
            {
                // Ignore detection failures; treat as not running.
            }

            // Fallback: enumerate and compare ProcessName.
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (string.Equals(p.ProcessName, procName, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }

        public void WriteBackupStopLog(string backupName, BackupStopReason reason, string? currentFile = null)
        {
            string spec = GetBusinessSoftwareSpec();
            string why = reason switch
            {
                BackupStopReason.UserRequested => "STOP: user requested",
                BackupStopReason.BusinessSoftwareDetected => $"STOP: business software detected ({spec})",
                _ => "STOP"
            };

            WriteLogEntry(new LogEntry
            {
                Timestamp = DateTime.Now,
                BackupName = backupName ?? "",
                SourcePath = currentFile ?? "",
                DestinationPath = why,
                FileSize = 0,
                TransferTimeMs = -2,
                EncryptionTimeMs = 0
            });
        }

        // Marker to make encryption idempotent in EasySave:
        // XOR encryption is symmetric; applying it twice with the same key would decrypt.
        //
        // Encrypted file format (v2):
        //   line1: EASYSAVECRYPT2
        //   line2: <sha256 hex of original plaintext>
        //   line3+: encrypted bytes
        //
        // We keep v1 detection for backward compatibility (no hash line).
        private const string EasySaveCryptoMagicV1 = "EASYSAVECRYPT1";
        private const string EasySaveCryptoMagicV2 = "EASYSAVECRYPT2";

        private static bool TryReadEasySaveCryptoHeader(string filePath, out bool isEncrypted, out string? plaintextSha256Hex)
        {
            isEncrypted = false;
            plaintextSha256Hex = null;

            try
            {
                // Read only a small prefix and parse the first 1-2 lines.
                byte[] buf = new byte[256];
                int read;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    read = fs.Read(buf, 0, buf.Length);
                }
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

        private static bool LooksLikePlaintextJson(string filePath)
        {
            try
            {
                byte[] data;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int max = (int)Math.Min(4096, fs.Length);
                    data = new byte[max];
                    int read = fs.Read(data, 0, max);
                    if (read <= 0) return false;
                    if (read != max) Array.Resize(ref data, read);
                }

                int i = 0;
                // UTF-8 BOM
                if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) i = 3;

                for (; i < data.Length; i++)
                {
                    byte b = data[i];
                    if (b == (byte)' ' || b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n')
                        continue;

                    return b == (byte)'{' || b == (byte)'[';
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikePlaintextXml(string filePath)
        {
            try
            {
                byte[] data;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int max = (int)Math.Min(4096, fs.Length);
                    data = new byte[max];
                    int read = fs.Read(data, 0, max);
                    if (read <= 0) return false;
                    if (read != max) Array.Resize(ref data, read);
                }

                int i = 0;
                // UTF-8 BOM
                if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) i = 3;

                for (; i < data.Length; i++)
                {
                    byte b = data[i];
                    if (b == (byte)' ' || b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n')
                        continue;

                    return b == (byte)'<';
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikePlaintextText(string filePath)
        {
            try
            {
                byte[] data;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int max = (int)Math.Min(4096, fs.Length);
                    data = new byte[max];
                    int read = fs.Read(data, 0, max);
                    if (read <= 0) return false;
                    if (read != max) Array.Resize(ref data, read);
                }

                int bad = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    byte b = data[i];
                    if (b == 0) return false; // NUL is extremely unlikely in plaintext configs
                    if (b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n') continue;

                    // Count control chars as "bad". Allow >= 0x20 or >= 0x80 (UTF-8 bytes).
                    if (b < 0x20) bad++;
                }

                // If too many control characters, it's likely encrypted/binary.
                return ((double)bad / Math.Max(1, data.Length)) <= 0.05;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTextLikeExtension(string ext)
        {
            return ext == ".txt" ||
                   ext == ".json" ||
                   ext == ".xml" ||
                   ext == ".csv" ||
                   ext == ".log" ||
                   ext == ".ini" ||
                   ext == ".md" ||
                   ext == ".yaml" ||
                   ext == ".yml" ||
                   ext == ".config";
        }

        private static bool IsLikelyAlreadyEncryptedTextByHeuristic(string filePath, string ext)
        {
            // Only apply heuristics to known text-like formats to avoid skipping encryption on binaries.
            if (!IsTextLikeExtension(ext)) return false;

            if (ext == ".json") return !LooksLikePlaintextJson(filePath);
            if (ext == ".xml") return !LooksLikePlaintextXml(filePath);
            return !LooksLikePlaintextText(filePath);
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

        private bool ShouldEncryptFile(string filePath)
        {
            if (!config.EnableEncryption) return false;

            string ext = NormalizeExtension(Path.GetExtension(filePath));
            if (string.IsNullOrWhiteSpace(ext)) return false;

            // Normalize configured extensions once per call (small lists expected).
            bool extMatch = false;
            foreach (var configured in config.EncryptionExtensions ?? new List<string>())
            {
                if (ext == NormalizeExtension(configured))
                {
                    extMatch = true;
                    break;
                }
            }

            if (!extMatch) return false;

            // Already encrypted by EasySave => do not re-encrypt (prevents XOR "decrypt on second pass").
            if (HasEasySaveCryptoHeader(filePath)) return false;

            // Heuristic guard to avoid decrypting files that are already XOR-encrypted without the header.
            // Only apply for text-like formats where we can reasonably detect plaintext.
            if (IsLikelyAlreadyEncryptedTextByHeuristic(filePath, ext)) return false;

            return true;
        }

        private string? ResolveCryptoSoftExecutablePath()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(config.CryptoSoftPath) && File.Exists(config.CryptoSoftPath))
                {
                    return config.CryptoSoftPath;
                }

                // Try to locate CryptoSoft.exe inside the repository layout.
                string[] roots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
                foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    var dir = new DirectoryInfo(root);
                    for (int i = 0; i < 6 && dir is not null; i++)
                    {
                        string cryptoBin = Path.Combine(dir.FullName, "CryptoSoft", "bin");
                        if (Directory.Exists(cryptoBin))
                        {
                            var candidates = Directory.GetFiles(cryptoBin, "CryptoSoft.exe", SearchOption.AllDirectories);
                            var best = candidates
                                .Select(p => new FileInfo(p))
                                .OrderByDescending(f => f.LastWriteTimeUtc)
                                .FirstOrDefault();

                            if (best is not null && best.Exists)
                            {
                                return best.FullName;
                            }
                        }

                        dir = dir.Parent;
                    }
                }
            }
            catch
            {
                // Ignore detection failures.
            }

            return null;
        }

        private bool TryEncryptInPlaceWithCryptoSoft(string targetFilePath, string? plaintextHashHex, out long encryptionTimeMs, out string? error)
        {
            encryptionTimeMs = 0;
            error = null;

            // If the file is already encrypted by EasySave, do nothing.
            if (HasEasySaveCryptoHeader(targetFilePath))
            {
                encryptionTimeMs = 0;
                return true;
            }

            string? cryptoSoftExe = ResolveCryptoSoftExecutablePath();
            if (string.IsNullOrWhiteSpace(cryptoSoftExe))
            {
                encryptionTimeMs = -99;
                error = "CryptoSoft executable not found.";
                return false;
            }

            string tempOut = targetFilePath + ".cryptosoft_tmp";
            string tempFinal = targetFilePath + ".easysavecrypt_tmp";

            try
            {
                // If the hash is missing, compute it from the plaintext file (rare; normally computed during copy).
                plaintextHashHex ??= ComputeSha256Hex(targetFilePath);

                var psi = new ProcessStartInfo
                {
                    FileName = cryptoSoftExe,
                    Arguments = $"\"{targetFilePath}\" \"{tempOut}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                var sw = Stopwatch.StartNew();
                using var proc = Process.Start(psi);
                if (proc is null)
                {
                    encryptionTimeMs = -99;
                    error = "CryptoSoft failed to start.";
                    return false;
                }

                // Wait with polling so the UI "Stop" can cancel encryption immediately if needed.
                while (true)
                {
                    if (proc.WaitForExit(200))
                        break;

                    // Only the user stop cancels immediately; business-software stop finishes current file.
                    if (IsStopRequested() && PeekStopReason() == BackupStopReason.UserRequested)
                    {
                        try { proc.Kill(entireProcessTree: true); } catch { }
                        encryptionTimeMs = -98;
                        error = "Encryption cancelled by user.";
                        try { if (File.Exists(tempOut)) File.Delete(tempOut); } catch { }
                        try { if (File.Exists(tempFinal)) File.Delete(tempFinal); } catch { }
                        return false;
                    }
                }
                sw.Stop();

                int exitCode = proc.ExitCode;
                if (exitCode < 0)
                {
                    encryptionTimeMs = exitCode; // keep CryptoSoft error codes (<0)
                    error = $"CryptoSoft error (code {exitCode}).";

                    try { if (File.Exists(tempOut)) File.Delete(tempOut); } catch { }
                    try { if (File.Exists(tempFinal)) File.Delete(tempFinal); } catch { }
                    return false;
                }

                // Success: ensure >0ms when encryption occurred (0 means "no encryption" per spec).
                encryptionTimeMs = exitCode > 0 ? exitCode : Math.Max(1, sw.ElapsedMilliseconds);

                if (!File.Exists(tempOut))
                {
                    encryptionTimeMs = -3;
                    error = "CryptoSoft reported success but did not produce output.";
                    return false;
                }

                // Replace the copied file with its encrypted version + EasySave header (idempotence + hash).
                // Format: "EASYSAVECRYPT2\n" + "<sha256hex>\n" + encrypted bytes.
                using (var outFs = new FileStream(tempFinal, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] h1 = Encoding.ASCII.GetBytes(EasySaveCryptoMagicV2 + "\n");
                    byte[] h2 = Encoding.ASCII.GetBytes(plaintextHashHex + "\n");
                    outFs.Write(h1, 0, h1.Length);
                    outFs.Write(h2, 0, h2.Length);

                    using var inFs = new FileStream(tempOut, FileMode.Open, FileAccess.Read, FileShare.Read);
                    inFs.CopyTo(outFs);
                    outFs.Flush(true);
                }

                File.Move(tempFinal, targetFilePath, overwrite: true);
                try { File.Delete(tempOut); } catch { }

                return true;
            }
            catch (Exception ex)
            {
                encryptionTimeMs = -99;
                error = ex.Message;
                try { if (File.Exists(tempOut)) File.Delete(tempOut); } catch { }
                try { if (File.Exists(tempFinal)) File.Delete(tempFinal); } catch { }
                return false;
            }
        }

        private void WriteLogEntry(LogEntry entry)
        {
            try
            {
                xmlLogger?.Write(entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing XML log: {ex.Message}");
            }

            try
            {
                jsonLogger?.Write(entry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing JSON log: {ex.Message}");
            }
        }
    }
}
