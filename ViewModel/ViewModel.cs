using System;
using System.IO;
using System.Collections.Generic;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using System.Threading;
using easySave_BMT.Resources_;

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
        /// </summary>
        public void RunApp()
        {
            /// <see cref="Model.CreateLogs"/> returns 100 on success
            int loadResult = model.CreateLogs();

            if (loadResult == 100)
            {
                /// <see cref="ResourceManager.GetString"/> for localized strings
                Console.WriteLine(ResourceManager.GetString("FileAddedSuccess"));
                view.DisplayMessage(100);  // Welcome banner
            }
            else
            {
                Console.WriteLine(ResourceManager.GetString("Error"));
                view.DisplayMessage(loadResult);
            }

            /// <summary>Main application loop</summary>
            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                /// <see cref="View.Menu"/> returns 1-6 based on user choice
                switch (this.view.Menu())
                {
                    case 1:
                        DisplaySaves();     // List all backup saves
                        break;
                    case 2:
                        AddSave();          // Create new backup job
                        break;
                    case 3:
                        RemoveSave();       // Delete backup job
                        break;
                    case 4:
                        LaunchBackupsave(); // Execute backup(s)
                        break;
                    case 5:
                        ConfigurationMenu(); // Settings menu
                        break;
                    case 6:
                        currentlyRunning = false;
                        Console.WriteLine(ResourceManager.GetString("Quit") + "!");
                        Console.WriteLine(ResourceManager.GetString("PressEnter"));
                        Console.ReadKey();
                        break;
                    default:
                        this.view.DisplayMessage(206);  // Invalid option
                        break;
                }
            }
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
                            view.DisplayMessage(218);  // Success message
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
                        inConfigMenu = false;  // Back to main menu
                        break;

                    default:
                        view.DisplayMessage(206);  // Invalid config option
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
                    this.view.DisplayMessage(204);  // "List is empty"
                }
            }
            else
            {
                this.view.DisplayMessage(reloadResult);  // Load error
            }
        }

        /// <summary>
        /// Adds new backup save with validation (max 5 saves)
        /// Gets name/source/dest/type from View with user cancellation support
        /// </summary>
        private void AddSave()
        {
            if (this.model.saves.Count < 5)  // Maximum 5 backup jobs
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
                        return;  // User cancelled
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
                this.view.DisplayMessage(205);  // "List is full"
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

                int index = choice - 1;  // 1-based → 0-based index

                if (index >= 0 && index < this.model.saves.Count)
                {
                    /// <see cref="Model.RemoveSave"/> returns result code (103=success)
                    this.view.DisplayMessage(model.RemoveSave(index));
                }
                else
                {
                    this.view.DisplayMessage(206);  // Invalid index
                }
            }
            else
            {
                this.view.DisplayMessage(204);  // Empty list
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
                        return;  // Cancel

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

                            this.view.DisplayMessage(4);  // "Press Enter to continue"
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
                this.view.DisplayMessage(1);  // "Back to menu"
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
                    return 208;  // Invalid backup type
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
                return 210;  // Cannot create backup directory
            }

            Console.Clear();
            
            /// <see cref="CopyFiles"/> returns list of failed files
            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst);
            DateTime endTime = DateTime.Now;
            TimeSpan saveTime = endTime - startTime;
            double transferTime = saveTime.TotalMilliseconds;

            /// <summary>Log backup completion to JSON file</summary>
            this.model.AddLogInJSONFile();
            this.view.DisplayMessage(3);  // "Backup information:"

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
