using System;
using System.IO;
using System.Collections.Generic;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using System.Threading;
using easySave_BMT.Resources_;

namespace easySave_BMT.ViewModel_
{
    public class ViewModel
    {
        public Model model;
        public View view;

        public ViewModel()
        {
            this.model = new Model();
            this.view = new View(this);
        }

        public void RunApp()
        {
            int loadResult = model.CreateLogs();

            if (loadResult == 100)
            {
                Console.WriteLine(ResourceManager.GetString("FileAddedSuccess"));
                view.DisplayMessage(100);
            }
            else
            {
                Console.WriteLine(ResourceManager.GetString("Error"));
                view.DisplayMessage(loadResult);
            }

            bool currentlyRunning = true;
            while (currentlyRunning)
            {
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

        private void ConfigurationMenu()
        {
            bool inConfigMenu = true;

            while (inConfigMenu)
            {
                int choice = view.ConfigurationMenu();

                switch (choice)
                {
                    case 1:
                        var config = model.GetConfig();
                        view.DisplayCurrentConfiguration(config);
                        break;

                    case 2:
                        string newLogDir = view.AskForLogDirectory();
                        if (!string.IsNullOrWhiteSpace(newLogDir))
                        {
                            model.UpdateConfig(newLogDir, null, null);
                            view.DisplayMessage(218);
                        }
                        break;

                    case 3:
                        string newStatePath = view.AskForStateFilePath();
                        if (!string.IsNullOrWhiteSpace(newStatePath))
                        {
                            model.UpdateConfig(null, newStatePath, null);
                            view.DisplayMessage(218);
                        }
                        break;

                    case 4:
                        string newLang = view.AskForLanguage();
                        if (!string.IsNullOrWhiteSpace(newLang))
                        {
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

        private void DisplaySaves()
        {
            int reloadResult = this.model.ReloadSavesFromFile();

            if (reloadResult == 100)
            {
                if (this.model.saves.Count > 0)
                {
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

        private void AddSave()
        {
            if (this.model.saves.Count < 5)
            {
                string addSaveName = view.SaveName();
                if (addSaveName == "0") return;

                string addSaveSrc = view.SaveSrc();
                if (addSaveSrc == "0") return;

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
                this.view.DisplayMessage(model.AddSave(addSaveName, addSaveSrc, addSaveDest, AddSaveBackupType));
            }
            else
            {
                this.view.DisplayMessage(205);
            }
        }

        private void RemoveSave()
        {
            if (this.model.saves.Count > 0)
            {
                int choice = view.RemovesaveChoice();
                if (choice == 0) return;

                int index = choice - 1;

                if (index >= 0 && index < this.model.saves.Count)
                {
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
                        foreach (Save save in this.model.saves)
                        {
                            int result = LaunchBackupType(save);
                            this.view.DisplayMessage(result);

                            if (result == 104 || result == 105 || result == 216)
                            {
                                model.FinishBackup(save);
                            }

                            this.view.DisplayMessage(4);
                        }
                        break;

                    default:
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

        public int LaunchBackupType(Save _save)
        {
            DirectoryInfo dir = new DirectoryInfo(_save.src);

            if (!dir.Exists && !Directory.Exists(_save.dst))
            {
                return 207;
            }

            var activeState = new State(0, 0, _save.src, _save.dst);
            _save.state = activeState;
            model.UpdateSaveState(_save);

            switch (_save.backupType)
            {
                case BackupType.DIFFERENTIAL:
                    string fullBackupDir = GetFullBackupDir(_save);

                    if (fullBackupDir != null)
                    {
                        return DifferentialBackupSetup(_save, dir, fullBackupDir);
                    }
                    return FullBackupSetup(_save, dir);

                case BackupType.FULL:
                    return FullBackupSetup(_save, dir);

                default:
                    return 208;
            }
        }

        private string GetFullBackupDir(Save _save)
        {
            DirectoryInfo[] dirs = new DirectoryInfo(_save.dst).GetDirectories();

            foreach (DirectoryInfo directory in dirs)
            {
                if (directory.Name.IndexOf("_") > 0 &&
                    _save.name == directory.Name.Substring(0, directory.Name.IndexOf("_")))
                {
                    return directory.FullName;
                }
            }
            return null;
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
                string currFullBackPath = _fullBackupDir + "\\" + Path.GetRelativePath(_save.src, file.FullName);

                if (!File.Exists(currFullBackPath) || !IsSameFile(currFullBackPath, file.FullName))
                {
                    totalSize += file.Length;
                    filesToCopy.Add(file);
                }
            }

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

        private bool IsSameFile(string path1, string path2)
        {
            try
            {
                byte[] file1 = File.ReadAllBytes(path1);
                byte[] file2 = File.ReadAllBytes(path2);

                if (file1.Length == file2.Length)
                {
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

        private int DoBackup(Save _save, FileInfo[] _files, long _totalSize)
        {
            DateTime startTime = DateTime.Now;
            string dst = _save.dst + _save.name + "_" + startTime.ToString("yyyy-MM-dd_HH-mm-ss") + "\\";
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
            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst);
            DateTime endTime = DateTime.Now;
            TimeSpan saveTime = endTime - startTime;
            double transferTime = saveTime.TotalMilliseconds;

            this.model.AddLogInJSONFile();
            this.view.DisplayMessage(3);

            foreach (string failedFile in failedFiles)
            {
                this.view.DisplayFiledError(failedFile);
            }
            this.view.DisplayBackupRecap(_save.name, transferTime);

            if (failedFiles.Count == 0)
            {
                return 104;
            }
            else
            {
                return 216;
            }
        }

        private List<string> CopyFiles(Save _save, FileInfo[] _files, long _totalSize, string _dst)
        {
            long leftSize = _totalSize;
            int totalFile = _files.Length;
            List<string> failedFiles = new List<string>();

            for (int i = 0; i < _files.Length; i++)
            {
                int pourcent = ((i + 1) * 100) / totalFile;
                long curSize = _files[i].Length;
                leftSize -= curSize;

                if (this.model.CopyFile(_save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent))
                {
                    Thread.Sleep((int)(curSize / 1000000));
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
