using System;                      // Library .NET
using System.IO;                   // files/directory management
using System.Collections.Generic;  // List<list>, dictionnary etc.
using easySave_BMT.Model_;         // Importation of "Model" directory class(es)
using easySave_BMT.View_;
using System.Xml.Linq;             // Importation of "View" directory class(es)


namespace easySave_BMT.ViewModel_  // Creation of ViewModel namespace
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
            // Load existing saves from JSON file
            int loadResult = model.CreateLogs();

            if (loadResult == 100)
            {
                Console.WriteLine("Application EasySave - BMT chargée avec succès !");
                view.DisplayMessage(100);
            }
            else
            {
                Console.WriteLine("Erreur lors du chargement des sauvegardes.");
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
                        // TODO: configuration / langue
                        break;
                    case 6:
                        currentlyRunning = false; // quitte la page
                        Console.WriteLine("Merci d'avoir utilisé EasySave - BMT !");
                        Console.WriteLine("Appuyez sur une touche pour quitter...");
                        Console.ReadKey();
                        break;
                    default:
                        this.view.DisplayMessage(206); // Invalid choice
                        break;
                }
            }
        }

        private void DisplaySaves() // Method used in case 1, used to display all saves jobs
        {
            // CRITICAL FIX: Always reload from JSON before displaying
            int reloadResult = this.model.ReloadSavesFromFile();

            if (reloadResult == 100) // Success
            {
                if (this.model.saves.Count > 0)
                {
                    this.view.DisplayAllSaves();
                }
                else
                {
                    this.view.DisplayMessage(204); // Empty list
                }
            }
            else
            {
                this.view.DisplayMessage(reloadResult); // Display error code
            }
        }

        private void AddSave() // method used in case 2, used to add a new save job
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

        //Remove a save
        private void RemoveSave()
        {
            if (this.model.saves.Count > 0)
            {
                int choice = view.RemovesaveChoice();
                if (choice == 0) return; // User chose to go back

                // Adjust for 1-based indexing in display
                int index = choice - 1;

                if (index >= 0 && index < this.model.saves.Count)
                {
                    this.view.DisplayMessage(model.RemoveSave(index));
                }
                else
                {
                    this.view.DisplayMessage(206); // Invalid choice
                }
            }
            else
            {
                this.view.DisplayMessage(204); // Empty list
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
                            this.view.DisplayMessage(LaunchBackupType(save));
                            this.view.DisplayMessage(4);
                        }
                        break;

                    default:
                        int indexsave = userChoice - 2;
                        this.view.DisplayMessage(LaunchBackupType(this.model.saves[indexsave]));
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

            List<string> failedFiles = CopyFiles(_save, _files, _totalSize, dst);
            DateTime endTime = DateTime.Now;
            TimeSpan saveTime = endTime - startTime;
            double transferTime = saveTime.TotalMilliseconds;
            _save.state = null;
            this.model.AddLogInJSONFile();
            this.view.DisplayMessage(3);

            foreach (string failedFile in failedFiles)
            {
                this.view.DisplayFiledError(failedFile);
            }
            this.view.DisplayBackupRecap(_save.name, transferTime);

            if (failedFiles.Count == 0)
            {
                // Return Success Code
                return 104;
            }
            else
            {
                // Return Error Code
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
                int pourcent = ((i + 1) * 100) / totalFile;  // +1 pour réalisme
                long curSize = _files[i].Length;
                leftSize -= curSize;

                if (this.model.CopyFile(_save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent))
                {
                    // SIMULE réalisme : délai proportionnel à taille
                    Thread.Sleep((int)(curSize / 1000000));  // 1ms par Mo
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
