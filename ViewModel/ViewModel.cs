using System;
using System.Collections.ObjectModel;
using easySave_BMT.Model_;
using easySave_BMT.Resources_;
using easySave_BMT.View_;
using easySave_BMT.ViewModel_.Backup;
using easySave_BMT.ViewModel_.CommandLine;
using easySave_BMT.ViewModel_.Configuration;
using easySave_BMT.ViewModel_.Saves;

namespace easySave_BMT.ViewModel_
{
    /// <summary>
    /// Main coordinator between model and presentation layers.
    /// It orchestrates console and GUI application flows while preserving MVVM boundaries.
    /// </summary>
    public class ViewModel
    {
        /// <summary>
        /// Gets the core domain model facade.
        /// </summary>
        public Model Model { get; }

        /// <summary>
        /// Gets the console view adapter.
        /// </summary>
        public View View { get; }

        /// <summary>
        /// Gets or sets the optional GUI progress observer used by Avalonia.
        /// </summary>
        public IProgressObserverGUI? GuiView { get; set; }

        /// <summary>
        /// Gets the command-line parser service.
        /// </summary>
        public CommandLineParser CommandLineParser { get; }

        /// <summary>
        /// Gets the command-line backup execution service.
        /// </summary>
        public CommandLineBackupRunner CommandLineRunner { get; }

        /// <summary>
        /// Gets the configuration controller.
        /// </summary>
        public ConfigurationController ConfigController { get; }

        /// <summary>
        /// Gets the save list controller.
        /// </summary>
        public SaveListManager SaveListManager { get; }

        /// <summary>
        /// Gets the save creation and removal controller.
        /// </summary>
        public SaveManager SaveManager { get; }

        /// <summary>
        /// Gets the backup launch service.
        /// </summary>
        public BackupLauncher BackupLauncher { get; }

        /// <summary>
        /// Gets a new collection for GUI binding snapshots.
        /// </summary>
        public ObservableCollection<Model_.Save> Saves => new(Model.saves);

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel"/> class.
        /// </summary>
        public ViewModel()
        {
            Model = new Model();
            View = new View(this);

            CommandLineParser = new CommandLineParser();
            BackupLauncher = new BackupLauncher(Model, View, () => GuiView);
            CommandLineRunner = new CommandLineBackupRunner(Model, BackupLauncher);
            ConfigController = new ConfigurationController(View, Model);
            SaveListManager = new SaveListManager(Model, View, () => GuiView, RefreshGuiSaves);
            SaveManager = new SaveManager(View, Model);
        }

        /// <summary>
        /// Runs the console application workflow.
        /// </summary>
        public void RunApp()
        {
            int loadResult = Model.CreateLogs();

            if (loadResult == 100)
            {
                Console.WriteLine(ResourceManager.GetString("FileAddedSuccess"));
            }
            else
            {
                Console.WriteLine(ResourceManager.GetString("Error"));
                View.DisplayMessage(loadResult);
            }

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                CommandLineParser.HandleCommandLine(args, CommandLineRunner);
                return;
            }

            View.DisplayMessage(100);

            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                switch (View.Menu())
                {
                    case 1:
                        SaveListManager.DisplaySaves();
                        break;
                    case 2:
                        SaveManager.AddSave();
                        break;
                    case 3:
                        SaveManager.RemoveSave();
                        break;
                    case 4:
                        BackupLauncher.LaunchBackupsave();
                        break;
                    case 5:
                        ConfigController.ConfigurationMenu();
                        break;
                    case 6:
                        currentlyRunning = false;
                        Console.WriteLine(ResourceManager.GetString("Quit") + "!");
                        Console.WriteLine(ResourceManager.GetString("PressEnter"));
                        Console.ReadKey();
                        break;
                    default:
                        View.DisplayMessage(206);
                        break;
                }
            }
        }

        /// <summary>
        /// Runs the GUI workflow and initializes the GUI observer binding.
        /// </summary>
        /// <param name="guiView">The GUI observer implementation.</param>
        public void RunAppGUI(IProgressObserverGUI guiView)
        {
            GuiView = guiView;

            int loadResult = Model.CreateLogs();
            if (loadResult != 100)
            {
                guiView.ShowMessage(Resources_.ResourceManager.GetString("Error"));
                return;
            }

            guiView.ShowMessage(Resources_.ResourceManager.GetString("UiReady"));
            SaveListManager.DisplaySaves();
        }

        /// <summary>
        /// Pushes the latest save list from the model to the active GUI observer.
        /// </summary>
        public void RefreshGuiSaves()
        {
            if (GuiView == null)
            {
                return;
            }

            var target = GuiView.Saves;
            target.Clear();
            foreach (var save in Model.saves)
            {
                target.Add(save);
            }
        }
    }

    /// <summary>
    /// Defines GUI progress callbacks for Avalonia binding.
    /// </summary>
    public interface IProgressObserverGUI
    {
        /// <summary>
        /// Notifies the observer about a progress update.
        /// </summary>
        void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent);

        /// <summary>
        /// Notifies the observer that a backup has completed.
        /// </summary>
        void OnBackupComplete(string backupName, double transferTime);

        /// <summary>
        /// Notifies the observer that a file operation failed.
        /// </summary>
        void OnFileError(string fileName);

        /// <summary>
        /// Notifies the observer of encryption summary counters.
        /// </summary>
        void OnEncryptionSummary(string backupName, int encryptedCount, int skippedAlreadyEncryptedCount);

        /// <summary>
        /// Displays a user-facing message in the GUI.
        /// </summary>
        void ShowMessage(string message);

        /// <summary>
        /// Gets the collection bound to the GUI save list.
        /// </summary>
        ObservableCollection<Model_.Save> Saves { get; }
    }
}