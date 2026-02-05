using System;
using System.IO;
using System.Collections.Generic;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using System.Threading;
using easySave_BMT.Resources_;
using System.Linq;

namespace easySave_BMT.ViewModel_
{
    /// <summary>
    /// Main ViewModel class that connects View layer with Model layer
    /// Handles all application logic and user interactions
    /// </summary>
    public class ViewModel
    {
        /// <summary>
        /// Model instance to access backup saves and configuration data
        /// </summary>
        public Model model;

        /// <summary>
        /// View instance to display menus, messages and progress to user
        /// </summary>
        public View view;

        /// <summary>
        /// Constructor initializes Model and View instances
        /// Creates dependency between ViewModel and View (passes 'this')
        /// </summary>
        public ViewModel()
        {
            this.model = new Model();
            this.view = new View(this);
        }

        /// <summary>
        /// Main application entry point called from Program.cs
        /// Initializes logs, shows main menu loop until user quits
        /// Supports command line arguments for automatic backup execution:
        /// - EasySave.exe 1;3;5 : Execute backups 1, 3 and 5
        /// - EasySave.exe 1-3;5 : Execute backups 1, 2, 3 and 5
        /// </summary>
        public void RunApp()
        {
            /// <see cref="Model.CreateLogs"/> returns 100 on success
            int loadResult = model.CreateLogs();

            if (loadResult == 100)
            {
                /// <see cref="ResourceManager.GetString"/> for localized strings
                Console.WriteLine(ResourceManager.GetString("FileAddedSuccess"));
            }
            else
            {
                Console.WriteLine(ResourceManager.GetString("Error"));
                view.DisplayMessage(loadResult);
            }

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length > 1)
            {
                // Utilise le deuxième argument directement (premier argument est le chemin de l'exécutable)
                string backupArg = args[1];
                
                // Vérifie s'il y a des arguments supplémentaires (avertissement)
                if (args.Length > 2)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nWarning: Only the first argument after the executable name is used.");
                    Console.WriteLine("Use semicolons (;) to separate multiple backup indices.");
                    Console.ResetColor();
                }
                
                List<int> backupIndices = ParseCommandLineArguments(backupArg);

                if (backupIndices != null && backupIndices.Count > 0)
                {
                    ExecuteCommandLineBackups(backupIndices);
                    return;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid command line argument format.");
                    Console.WriteLine("Usage examples:");
                    Console.WriteLine("  EasySave.exe 1;3;5       (execute backups 1, 3 and 5)");
                    Console.WriteLine("  EasySave.exe 1-3;5       (execute backups 1, 2, 3 and 5)");
                    Console.WriteLine("  EasySave.exe 1;2-4;7     (execute backups 1, 2, 3, 4 and 7)");
                    Console.WriteLine("  EasySave.exe 1-5         (execute backups 1 to 5)");
                    Console.ResetColor();
                    Console.WriteLine("\nPress Enter to continue to interactive menu...");
                    Console.ReadLine();
                }
            }

            view.DisplayMessage(100);

            /// <summary>Main application loop</summary>
            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                /// <see cref="View.Menu"/> returns 1-6 based on user choice
                switch (this.view.Menu())
                {
                    case 1:
                        DisplaySaves();
                        break;
                    case 2:
                        AddSave();
                        break;
                    case 3:
                        RemoveSave();
                        break;
                    case 4:
                        LaunchBackupsave();
                        break;
                    case 5:
                        ConfigurationMenu();
                        break;
                    case 6:
                        currentlyRunning = false;
                        Console.WriteLine(ResourceManager.GetString("Quit") + "!");
                        Console.WriteLine(ResourceManager.GetString("PressEnter"));
                        Console.ReadKey();
                        break;
                    default:
                        this.view.DisplayMessage(206);
                        break;
                }
            }
        }

        /// <summary>
        /// Parses command line arguments to extract backup indices
        /// Supports formats: "1-3" (range), "1;3" (list), "1;2-4;7" (mixed)
        /// </summary>
        /// <param name="argument">Command line argument string</param>
        /// <returns>List of backup indices (1-based) or null if invalid</returns>
        private List<int> ParseCommandLineArguments(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
                return null;

            List<int> indices = new List<int>();

            try
            {
                string[] parts = argument.Split(';');

                foreach (string part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                        continue;

                    if (part.Contains("-"))
                    {
                        string[] range = part.Split('-');
                        if (range.Length == 2)
                        {
                            int start = int.Parse(range[0].Trim());
                            int end = int.Parse(range[1].Trim());

                            if (start > 0 && end > 0 && start <= end)
                            {
                                for (int i = start; i <= end; i++)
                                {
                                    if (!indices.Contains(i))
                                        indices.Add(i);
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        int index = int.Parse(part.Trim());
                        if (index > 0 && !indices.Contains(index))
                            indices.Add(index);
                        else if (index <= 0)
                            return null;
                    }
                }

                indices.Sort();
                return indices;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Executes backups specified by command line arguments
        /// </summary>
        /// <param name="backupIndices">List of backup indices (1-based)</param>
        private void ExecuteCommandLineBackups(List<int> backupIndices)
        {
            Console.WriteLine("\n=== Automatic Backup Execution ===\n");

            if (this.model.saves.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No backup configurations found.");
                Console.ResetColor();
                return;
            }

            int successCount = 0;
            int errorCount = 0;
            List<string> executedBackups = new List<string>();
            List<string> failedBackups = new List<string>();

            foreach (int index in backupIndices)
            {
                int arrayIndex = index - 1;

                if (arrayIndex >= 0 && arrayIndex < this.model.saves.Count)
                {
                    Save save = this.model.saves[arrayIndex];
                    Console.WriteLine($"Executing backup {index}: {save.name}");

                    int result = LaunchBackupType(save);

                    if (result == 104 || result == 105)
                    {
                        model.FinishBackup(save);
                        successCount++;
                        executedBackups.Add($"{index} - {save.name}");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Backup {index} completed successfully\n");
                        Console.ResetColor();
                    }
                    else if (result == 216)
                    {
                        model.FinishBackup(save);
                        errorCount++;
                        failedBackups.Add($"{index} - {save.name} (partial)");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Backup {index} completed with errors\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        errorCount++;
                        failedBackups.Add($"{index} - {save.name}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ Backup {index} failed (Error {result})\n");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Backup index {index} does not exist (Available: 1-{this.model.saves.Count})\n");
                    Console.ResetColor();
                    errorCount++;
                    failedBackups.Add($"{index} - Not found");
                }
            }

            Console.WriteLine("\n=== Execution Summary ===");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successful: {successCount}");
            Console.ResetColor();

            if (errorCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed/Errors: {errorCount}");
                Console.ResetColor();
            }

            if (executedBackups.Count > 0)
            {
                Console.WriteLine("\nCompleted backups:");
                foreach (string backup in executedBackups)
                {
                    Console.WriteLine($"  ✓ {backup}");
                }
            }

            if (failedBackups.Count > 0)
            {
                Console.WriteLine("\nFailed backups:");
                foreach (string backup in failedBackups)
                {
                    Console.WriteLine($"  ✗ {backup}");
                }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Configuration submenu with loop until user returns (choice=0)
        /// Handles log directory, state file path and language changes
        /// </summary>
        private void ConfigurationMenu()
        {
            bool inConfigMenu = true;

            while (inConfigMenu)
            {
                /// <see cref="View.ConfigurationMenu"/> shows interactive config menu
                int choice = view.ConfigurationMenu();

                switch (choice)
                {
                    case 1:
                        /// <see cref="Model.GetConfig"/> retrieves current settings
                        var config = model.GetConfig();
                        view.DisplayCurrentConfiguration(config);
                        break;

                    case 2:
                        /// <see cref="View.AskForLogDirectory"/> with path validation
                        string newLogDir = view.AskForLogDirectory();
                        if (!string.IsNullOrWhiteSpace(newLogDir))
                        {
                            /// <see cref="Model.UpdateConfig"/> first param = log directory
                            model.UpdateConfig(newLogDir, null, null);
                            view.DisplayMessage(218);
                        }
                        break;

                    case 3:
                        /// <see cref="View.AskForStateFilePath"/> validates .json extension
                        string newStatePath = view.AskForStateFilePath();
                        if (!string.IsNullOrWhiteSpace(newStatePath))
                        {
                            /// <see cref="Model.UpdateConfig"/> second param = state file
                            model.UpdateConfig(null, newStatePath, null);
                            view.DisplayMessage(218);
                        }
                        break;

                    case 4:
                        /// <see cref="View.AskForLanguage"/> returns "fr" or "en"
                        string newLang = view.AskForLanguage();
                        if (!string.IsNullOrWhiteSpace(newLang))
                        {
                            /// <see cref="Model.UpdateConfig"/> third param = language
                            model.UpdateConfig(null, null, newLang);
                            view.DisplayMessage(218);
                        }
                        break;

                    case 0:
                        inConfigMenu = false;
                        break;

                    default:
                        view.DisplayMessage(206);
                        break;
                }
            }
        }

        /// <summary>
        /// Reloads and displays all backup saves from JSON state file
        /// Handles empty list and load errors
        /// </summary>
        private void DisplaySaves()
        {
            /// <see cref="Model.ReloadSavesFromFile"/> refreshes saves list
            int reloadResult = this.model.ReloadSavesFromFile();

            if (reloadResult == 100)
            {
                if (this.model.saves.Count > 0)
                {
                    /// <see cref="View.DisplayAllSaves"/> shows formatted list
                    this.view.DisplayAllSaves();
                }
                else
                {
                    this.view.DisplayMessage(204);
                }
            }
            else
            {
                this.view.DisplayMessage(reloadResult);
            }
        }

        /// <summary>
        /// Adds new backup save with validation (max 5 saves)
        /// Gets name/source/dest/type from View with user cancellation support
        /// </summary>
        private void AddSave()
        {
            if (this.model.saves.Count < 5)
            {
                /// <see cref="View.SaveName"/> validates length 1-20 + unique name
                string addSaveName = view.SaveName();
                if (addSaveName == "0") return;

                /// <see cref="View.SaveSrc"/> validates directory exists
                string addSaveSrc = view.SaveSrc();
                if (addSaveSrc == "0") return;

                /// <see cref="View.SaveDst"/> validates dest != source + permissions
                string addSaveDest = view.SaveDst(addSaveSrc);
                if (addSaveDest == "0") return;

                BackupType AddSaveBackupType;
                switch (view.AddSaveBackupType())
                {
                    case 0:
                        return;
                    case 1:
                        AddSaveBackupType = BackupType.FULL;
                        break;
                    case 2:
                        AddSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                    default:
                        AddSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                }

                /// <see cref="Model.AddSave"/> returns result code (101=success)
                this.view.DisplayMessage(model.AddSave(addSaveName, addSaveSrc, addSaveDest, AddSaveBackupType));
            }
            else
            {
                this.view.DisplayMessage(205);
            }
        }

        /// <summary>
        /// Removes backup save by index with bounds checking
        /// </summary>
        private void RemoveSave()
        {
            if (this.model.saves.Count > 0)
            {
                /// <see cref="View.RemovesaveChoice"/> shows numbered list
                int choice = view.RemovesaveChoice();
                if (choice == 0) return;

                int index = choice - 1;

                if (index >= 0 && index < this.model.saves.Count)
                {
                    /// <see cref="Model.RemoveSave"/> returns result code (103=success)
                    this.view.DisplayMessage(model.RemoveSave(index));
                }
                else
                {
                    this.view.DisplayMessage(206);
                }
            }
            else
            {
                this.view.DisplayMessage(204);
            }
        }

        /// <summary>
        /// Launches backup process for single save or all saves
        /// Calls <see cref="LaunchBackupType"/> for each selected save
        /// </summary>
        private void LaunchBackupsave()
        {
            if (this.model.saves.Count > 0)
            {
                int userChoice = view.LaunchBackupChoice();

                switch (userChoice)
                {
                    case 0:
                        return;

                    case 1:
                        /// <summary>Backup all saves in sequence</summary>
                        foreach (Save save in this.model.saves)
                        {
                            int result = LaunchBackupType(save);
                            this.view.DisplayMessage(result);

                            /// <summary>Update last backup date for success/partial results</summary>
                            if (result == 104 || result == 105 || result == 216)
                            {
                                model.FinishBackup(save);
                            }

                            this.view.DisplayMessage(4);
                        }
                        break;

                    default:
                        /// <summary>Backup single selected save</summary>
                        int indexsave = userChoice - 2;
                        Save selectedSave = this.model.saves[indexsave];
                        int backupResult = LaunchBackupType(selectedSave);
                        this.view.DisplayMessage(backupResult);

                        if (backupResult == 104 || backupResult == 105 || backupResult == 216)
                        {
                            model.FinishBackup(selectedSave);
                        }
                        break;
                }
                this.view.DisplayMessage(1);
            }
            else
            {
                this.view.DisplayMessage(204);
            }
        }

        /// <summary>
        /// Core backup logic dispatcher for FULL/DIFFERENTIAL types
        /// Initializes backup state and routes to correct backup method
        /// </summary>
        /// <param name="_save">Backup configuration</param>
        /// <returns>Result code: 104=success, 207=no source, 208=invalid type</returns>
        public int LaunchBackupType(Save _save)
        {
            DirectoryInfo dir = new DirectoryInfo(_save.src);

            /// <summary>Check source and destination directories exist</summary>
            if (!dir.Exists && !Directory.Exists(_save.dst))
            {
                return 207;
            }

            /// <summary>Initialize backup state tracking</summary>
            var activeState = new State(0, 0, _save.src, _save.dst);
            _save.state = activeState;
            model.UpdateSaveState(_save);

            switch (_save.backupType)
            {
                case BackupType.DIFFERENTIAL:
                    string fullBackupDir = GetFullBackupDir(_save);
                    if (fullBackupDir != null)
                    {
                        /// <see cref="DifferentialBackupSetup"/>
                        return DifferentialBackupSetup(_save, dir, fullBackupDir);
                    }
                    /// <see cref="FullBackupSetup"/> as fallback
                    return FullBackupSetup(_save, dir);

                case BackupType.FULL:
                    return FullBackupSetup(_save, dir);

                default:
                    return 208;
            }
        }

        /// <summary>
        /// Finds previous FULL backup directory for differential backup reference
        /// Searches for folder pattern: "SaveName_timestamp"
        /// </summary>
        private string GetFullBackupDir(Save _save)
        {
            DirectoryInfo[] dirs = new DirectoryInfo(_save.dst).GetDirectories();

            foreach (DirectoryInfo directory in dirs)
            {
                /// <summary>Match save name before first underscore</summary>
                if (directory.Name.IndexOf("_") > 0 &&
                    _save.name == directory.Name.Substring(0, directory.Name.IndexOf("_")))
                {
                    return directory.FullName;
                }
            }
            return null;
        }

        /// <summary>
        /// FULL backup setup - copies ALL files from source directory
        /// Calculates total size and calls main backup execution
        /// </summary>
        private int FullBackupSetup(Save _save, DirectoryInfo _dir)
        {
            long totalSize = 0;
            FileInfo[] files = _dir.GetFiles("*.*", SearchOption.AllDirectories);

            /// <summary>Calculate total backup size</summary>
            foreach (FileInfo file in files)
            {
                totalSize += file.Length;
            }
            return DoBackup(_save, files, totalSize);
        }

        /// <summary>
        /// DIFFERENTIAL backup setup - copies only changed/new files
        /// Compares source files with previous FULL backup content
        /// </summary>
        private int DifferentialBackupSetup(Save _save, DirectoryInfo _dir, string _fullBackupDir)
        {
            long totalSize = 0;
            FileInfo[] srcFiles = _dir.GetFiles("*.*", SearchOption.AllDirectories);
            List<FileInfo> filesToCopy = new List<FileInfo>();

            foreach (FileInfo file in srcFiles)
            {
                /// <summary>Build corresponding path in FULL backup</summary>
                string currFullBackPath = _fullBackupDir + "\\" + Path.GetRelativePath(_save.src, file.FullName);

                /// <summary>Copy if missing or content changed</summary>
                if (!File.Exists(currFullBackPath) || !IsSameFile(currFullBackPath, file.FullName))
                {
                    totalSize += file.Length;
                    filesToCopy.Add(file);
                }
            }

            /// <summary>No changes detected since last FULL backup</summary>
            if (filesToCopy.Count == 0)
            {
                _save.lastBackupDate = DateTime.Now.ToString("yyyy/MM/dd_HH:mm:ss");
                this.model.AddLogInJSONFile();
                this.view.DisplayMessage(3);
                this.view.DisplayBackupRecap(_save.name, 0);
                return 105;
            }
            return DoBackup(_save, filesToCopy.ToArray(), totalSize);
        }

        /// <summary>
        /// Byte-by-byte file comparison for differential backup detection
        /// Returns false on any content difference or read error
        /// </summary>
        private bool IsSameFile(string path1, string path2)
        {
            try
            {
                byte[] file1 = File.ReadAllBytes(path1);
                byte[] file2 = File.ReadAllBytes(path2);

                if (file1.Length == file2.Length)
                {
                    /// <summary>Compare every single byte</summary>
                    for (int i = 0; i < file1.Length; i++)
                    {
                        if (file1[i] != file2[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Main backup execution engine (FULL + DIFFERENTIAL)
        /// Creates timestamped folder, copies files, shows progress
        /// </summary>
        private int DoBackup(Save _save, FileInfo[] _files, long _totalSize)
        {
            DateTime startTime = DateTime.Now;

            /// <summary>Format: Destination/MySave_2024-01-15_14-30-00\</summary>
            string dst = _save.dst + _save.name + "_" + startTime.ToString("yyyy-MM-dd_HH-mm-ss") + "\\";

            /// <summary>Update state with total files/size for progress tracking</summary>
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

            Console.Clear();

            /// <see cref="CopyFiles"/> returns list of failed files
            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst);
            DateTime endTime = DateTime.Now;
            TimeSpan saveTime = endTime - startTime;
            double transferTime = saveTime.TotalMilliseconds;

            /// <summary>Log backup completion to JSON file</summary>
            this.model.AddLogInJSONFile();
            this.view.DisplayMessage(3);

            /// <summary>Display failed files list</summary>
            foreach (string failedFile in failedFiles)
            {
                this.view.DisplayFiledError(failedFile);
            }

            /// <summary>Show final statistics (time, total size)</summary>
            this.view.DisplayBackupRecap(_save.name, transferTime);

            /// <summary>Return codes: 104=full success, 216=partial with errors</summary>
            if (failedFiles.Count == 0)
            {
                return 104;
            }
            else
            {
                return 216;
            }
        }

        /// <summary>
        /// Copies files one-by-one with real-time progress display
        /// Simulates processing time based on file size
        /// </summary>
        private List<string> CopyFiles(Save _save, FileInfo[] _files, long _totalSize, string _dst)
        {
            long leftSize = _totalSize;
            int totalFile = _files.Length;
            List<string> failedFiles = new List<string>();

            /// <summary>Process each file sequentially</summary>
            for (int i = 0; i < _files.Length; i++)
            {
                /// <summary>Calculate progress percentage</summary>
                int pourcent = ((i + 1) * 100) / totalFile;
                long curSize = _files[i].Length;
                leftSize -= curSize;

                /// <see cref="Model.CopyFile"/> with progress parameters
                if (this.model.CopyFile(_save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent))
                {
                    /// <summary>Simulate real copy time (1ms per MB)</summary>
                    Thread.Sleep((int)(curSize / 1000000));
                    /// <see cref="View.DisplayCurrentState"/> updates progress bar
                    this.view.DisplayCurrentState(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);
                }
                else
                {
                    failedFiles.Add(_files[i].Name);
                }
            }
            return failedFiles;
        }
    }
}