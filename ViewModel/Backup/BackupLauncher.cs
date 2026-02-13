using System;
using System.IO;
using System.Linq;
using System.Threading;
using easySave_BMT.Model_;

namespace easySave_BMT.ViewModel_.Backup
{
    public class BackupLauncher
    {
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
            DirectoryInfo dir = new DirectoryInfo(_save.src);

            if (!dir.Exists && !Directory.Exists(_save.dst))
            {
                return 207;
            }

            var activeState = new State(0, 0, _save.src, _save.dst);
            _save.state = activeState;
            _viewModel.model.UpdateSaveState(_save);

            return ExecuteBackupStrategy(_save, dir);
        }

        private void BackupAllSaves()
        {
            foreach (Save save in _viewModel.model.saves)
            {
                int result = LaunchBackupType(save);
                _viewModel.view.DisplayMessage(result);

                if (result == 104 || result == 105 || result == 216)
                {
                    _viewModel.model.FinishBackup(save);
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

        private int ExecuteBackupStrategy(Save _save, DirectoryInfo _dir)
        {
            switch (_save.backupType)
            {
                case BackupType.DIFFERENTIAL:
                    string fullBackupDir = GetFullBackupDir(_save);
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
                _viewModel.model.AddLogInJSONFile();

                // Notification console
                _viewModel.view.DisplayMessage(3);
                _viewModel.view.DisplayBackupRecap(_save.name, 0);

                // Notification GUI éventuelle
                _viewModel.guiView?.OnBackupComplete(_save.name, 0);
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

                _viewModel.model.AddLogInJSONFile();

                // Notifications console
                _viewModel.view.DisplayMessage(3);

                foreach (string failedFile in failedFiles)
                {
                    _viewModel.view.DisplayFiledError(failedFile);
                    _viewModel.guiView?.OnFileError(failedFile);
                }

                _viewModel.view.DisplayBackupRecap(_save.name, transferTime);
                _viewModel.guiView?.OnBackupComplete(_save.name, transferTime);

            return failedFiles.Count == 0 ? 104 : 216;
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

                if (_viewModel.model.CopyFile(_save, _files[i], curSize, _dst, leftSize, totalFile, i, pourcent))
                {
                    Thread.Sleep((int)(curSize / 1000000));
                    // Mise à jour de la progression en console
                    _viewModel.view.DisplayCurrentState(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);

                    // Mise à jour de la progression en GUI (barre de progression / texte)
                    _viewModel.guiView?.OnProgressUpdate(_save.name, totalFile - i - 1, leftSize, curSize, pourcent);
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
