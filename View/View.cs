using easySave_BMT.Model_;
using easySave_BMT.ViewModel_;

namespace easySave_BMT.View_
{
    /// <summary>
    /// Console view adapter used by the core view model.
    /// </summary>
    public class View : IProgressObserver
    {
        private readonly ViewModel _viewModel;
        private readonly MenuDisplay _menuDisplay;
        private readonly MessageDisplay _messageDisplay;
        private readonly ValidationService _validationService;
        private readonly BackupInputService _backupInputService;
        private readonly ConfigurationInputService _configurationInputService;
        private readonly ProgressDisplay _progressDisplay;

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class.
        /// </summary>
        /// <param name="viewModel">The owning view model.</param>
        public View(ViewModel viewModel)
        {
            _viewModel = viewModel;
            _menuDisplay = new MenuDisplay();
            _messageDisplay = new MessageDisplay();
            _validationService = new ValidationService(_messageDisplay);
            _backupInputService = new BackupInputService(_validationService, _messageDisplay);
            _configurationInputService = new ConfigurationInputService(_validationService);
            _progressDisplay = new ProgressDisplay();
        }

        /// <summary>
        /// Shows the main menu.
        /// </summary>
        /// <returns>The selected option code.</returns>
        public int Menu()
        {
            return _menuDisplay.ShowMainMenu();
        }

        /// <summary>
        /// Shows the configuration menu.
        /// </summary>
        /// <returns>The selected option code.</returns>
        public int ConfigurationMenu()
        {
            return _menuDisplay.ShowConfigurationMenu();
        }

        /// <summary>
        /// Displays current configuration values.
        /// </summary>
        /// <param name="config">The current configuration.</param>
        public void DisplayCurrentConfiguration(Config config)
        {
            _configurationInputService.DisplayConfiguration(config);
        }

        /// <summary>
        /// Prompts for a log directory.
        /// </summary>
        /// <returns>The provided path or <c>null</c>.</returns>
        public string AskForLogDirectory()
        {
            return _configurationInputService.AskForLogDirectory();
        }

        /// <summary>
        /// Prompts for a state file path.
        /// </summary>
        /// <returns>The provided path or <c>null</c>.</returns>
        public string AskForStateFilePath()
        {
            return _configurationInputService.AskForStateFilePath(_viewModel.Model.GetConfig().StateFilePath);
        }

        /// <summary>
        /// Prompts for a UI language.
        /// </summary>
        /// <returns>The selected language code or <c>null</c>.</returns>
        public string AskForLanguage()
        {
            return _configurationInputService.AskForLanguage();
        }

        /// <summary>
        /// Displays a message using the existing message code system.
        /// </summary>
        /// <param name="id">The message code.</param>
        public void DisplayMessage(int id)
        {
            _messageDisplay.Display(id);
        }

        /// <summary>
        /// Asks for the backup type to create.
        /// </summary>
        /// <returns>The selected backup type code.</returns>
        public int AddSaveBackupType()
        {
            return _menuDisplay.ShowBackupTypeMenu();
        }

        /// <summary>
        /// Displays all configured saves.
        /// </summary>
        public void DisplayAllSaves()
        {
            _backupInputService.DisplayBackupsList(_viewModel.Model.saves);
        }

        /// <summary>
        /// Asks for a new save name.
        /// </summary>
        /// <returns>The save name or <c>"0"</c> to cancel.</returns>
        public string SaveName()
        {
            return _backupInputService.AskForBackupName(_viewModel.Model.saves);
        }

        /// <summary>
        /// Asks for a source path.
        /// </summary>
        /// <returns>The source path or <c>"0"</c> to cancel.</returns>
        public string SaveSrc()
        {
            return _backupInputService.AskForSourcePath();
        }

        /// <summary>
        /// Validates a destination path against a source path.
        /// </summary>
        /// <param name="src">The source directory.</param>
        /// <param name="dst">The destination directory.</param>
        /// <returns><c>true</c> when destination is valid.</returns>
        public bool ChecksaveDst(string src, string dst)
        {
            return _validationService.ValidateDestinationPath(src, dst);
        }

        /// <summary>
        /// Asks for a destination path.
        /// </summary>
        /// <param name="src">The source directory.</param>
        /// <returns>The destination path or <c>"0"</c> to cancel.</returns>
        public string SaveDst(string src)
        {
            return _backupInputService.AskForDestinationPath(src);
        }

        /// <summary>
        /// Displays the current backup progress in console mode.
        /// </summary>
        public void DisplayCurrentState(string name, int fileLeft, long leftSize, long curSize, int percent)
        {
            _progressDisplay.OnProgressUpdate(name, fileLeft, leftSize, curSize, percent);
        }

        /// <summary>
        /// Displays a backup completion summary in console mode.
        /// </summary>
        public void DisplayBackupRecap(string name, double transfertTime)
        {
            _progressDisplay.OnBackupComplete(name, transfertTime);
        }

        /// <summary>
        /// Displays a failed file notification in console mode.
        /// </summary>
        public void DisplayFiledError(string name)
        {
            _progressDisplay.OnFileError(name);
        }

        /// <summary>
        /// Asks the user which save to remove.
        /// </summary>
        /// <returns>The selected 1-based index or 0.</returns>
        public int RemovesaveChoice()
        {
            return _backupInputService.AskForBackupToDelete(_viewModel.Model.saves);
        }

        /// <summary>
        /// Asks the user which save(s) to launch.
        /// </summary>
        /// <returns>The selected menu value.</returns>
        public int LaunchBackupChoice()
        {
            return _backupInputService.AskForBackupToLaunch(_viewModel.Model.saves);
        }

        /// <inheritdoc />
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            _progressDisplay.OnProgressUpdate(backupName, filesLeft, sizeLeft, currentFileSize, percent);
        }

        /// <inheritdoc />
        public void OnBackupComplete(string backupName, double transferTime)
        {
            _progressDisplay.OnBackupComplete(backupName, transferTime);
        }

        /// <inheritdoc />
        public void OnFileError(string fileName)
        {
            _progressDisplay.OnFileError(fileName);
        }
    }
}
