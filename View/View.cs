using easySave_BMT.ViewModel_;
using easySave_BMT.Model_;

namespace easySave_BMT.View_
{
    public class View : IProgressObserver
    {
        private readonly ViewModel viewModel;
        private readonly MenuDisplay menuDisplay;
        private readonly MessageDisplay messageDisplay;
        private readonly ValidationService validationService;
        private readonly BackupInputService backupInputService;
        private readonly ConfigurationInputService configurationInputService;
        private readonly ProgressDisplay progressDisplay;

        public View(ViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.menuDisplay = new MenuDisplay();
            this.messageDisplay = new MessageDisplay();
            this.validationService = new ValidationService(messageDisplay);
            this.backupInputService = new BackupInputService(validationService, messageDisplay);
            this.configurationInputService = new ConfigurationInputService(validationService);
            this.progressDisplay = new ProgressDisplay();
        }

        public int Menu()
        {
            return menuDisplay.ShowMainMenu();
        }

        public int ConfigurationMenu()
        {
            return menuDisplay.ShowConfigurationMenu();
        }

        public void DisplayCurrentConfiguration(Config config)
        {
            configurationInputService.DisplayConfiguration(config);
        }

        public string AskForLogDirectory()
        {
            return configurationInputService.AskForLogDirectory();
        }

        public string AskForStateFilePath()
        {
            return configurationInputService.AskForStateFilePath(viewModel.model.GetConfig().StateFilePath);
        }

        public string AskForLanguage()
        {
            return configurationInputService.AskForLanguage();
        }

        public string AskForLogDestinationMode()
        {
            return configurationInputService.AskForLogDestinationMode(viewModel.model.GetConfig().LogDestinationMode);
        }

        public string AskForCentralizedLogEndpoint()
        {
            return configurationInputService.AskForCentralizedLogEndpoint(viewModel.model.GetConfig().CentralizedLogEndpoint);
        }

        public void DisplayMessage(int id)
        {
            messageDisplay.Display(id);
        }

        public int AddSaveBackupType()
        {
            return menuDisplay.ShowBackupTypeMenu();
        }

        public void DisplayAllSaves()
        {
            backupInputService.DisplayBackupsList(viewModel.model.saves);
        }

        public string SaveName()
        {
            return backupInputService.AskForBackupName(viewModel.model.saves);
        }

        public string SaveSrc()
        {
            return backupInputService.AskForSourcePath();
        }

        public bool ChecksaveDst(string src, string dst)
        {
            return validationService.ValidateDestinationPath(src, dst);
        }

        public string SaveDst(string src)
        {
            return backupInputService.AskForDestinationPath(src);
        }

        public void DisplayCurrentState(string name, int fileLeft, long leftSize, long curSize, int percent)
        {
            progressDisplay.OnProgressUpdate(name, fileLeft, leftSize, curSize, percent);
        }

        public void DisplayBackupRecap(string name, double transfertTime)
        {
            progressDisplay.OnBackupComplete(name, transfertTime);
        }

        public void DisplayFiledError(string name)
        {
            progressDisplay.OnFileError(name);
        }

        public int RemovesaveChoice()
        {
            return backupInputService.AskForBackupToDelete(viewModel.model.saves);
        }

        public int LaunchBackupChoice()
        {
            return backupInputService.AskForBackupToLaunch(viewModel.model.saves);
        }

        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            progressDisplay.OnProgressUpdate(backupName, filesLeft, sizeLeft, currentFileSize, percent);
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            progressDisplay.OnBackupComplete(backupName, transferTime);
        }

        public void OnFileError(string fileName)
        {
            progressDisplay.OnFileError(fileName);
        }
    }
}
