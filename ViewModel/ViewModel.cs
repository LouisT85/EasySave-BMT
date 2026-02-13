using System;
using System.Collections.ObjectModel;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using easySave_BMT.ViewModel_.CommandLine;
using easySave_BMT.ViewModel_.Configuration;
using easySave_BMT.ViewModel_.Saves;
using easySave_BMT.ViewModel_.Backup;
using easySave_BMT.Resources_;

namespace easySave_BMT.ViewModel_
{
    /// <summary>
    /// Main ViewModel class that connects View layer with Model layer
    /// Orchestrates all controllers and handles main application flow
    /// </summary>
    public class ViewModel
    {
        public Model model;
        public View view;
        public IProgressObserverGUI? guiView; // NOUVEAU : pour Avalonia

        // Controllers - publics pour accès interne
        public CommandLineParser commandLineParser;
        public CommandLineBackupRunner commandLineRunner;
        public ConfigurationController configController;
        public SaveListManager saveListManager;
        public SaveManager saveManager;
        public BackupLauncher backupLauncher;

        // NOUVEAU : ObservableCollection pour binding GUI
        public ObservableCollection<Model_.Save> Saves => new ObservableCollection<Model_.Save>(model.saves);

        public ViewModel()
        {
            this.model = new Model();
            this.view = new View(this);

            // Initialize controllers
            commandLineParser = new CommandLineParser();
            commandLineRunner = new CommandLineBackupRunner(this);
            configController = new ConfigurationController(this);
            saveListManager = new SaveListManager(this);
            saveManager = new SaveManager(this);
            backupLauncher = new BackupLauncher(this);
        }

        // CONSOLE INTACTE - AUCUN CHANGEMENT (sauf case 4 corrigé)
        public void RunApp()
        {
            int loadResult = model.CreateLogs();

            if (loadResult == 100)
            {
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
                commandLineParser.HandleCommandLine(args, commandLineRunner);
                return;
            }

            view.DisplayMessage(100);

            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                switch (this.view.Menu())
                {
                    case 1:
                        saveListManager.DisplaySaves();
                        break;
                    case 2:
                        saveManager.AddSave();
                        break;
                    case 3:
                        saveManager.RemoveSave();
                        break;
                    case 4:
                        backupLauncher.LaunchBackupsave(); // ✅ CORRIGÉ : sans paramètre
                        break;
                    case 5:
                        configController.ConfigurationMenu();
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

        // 🚀 NOUVEAU : Pour AVALONIA GUI
        public void RunAppGUI(IProgressObserverGUI guiView)
        {
            this.guiView = guiView;
            
            int loadResult = model.CreateLogs();
            if (loadResult != 100)
            {
                guiView.ShowMessage("❌ Erreur chargement logs");
                return;
            }

            guiView.ShowMessage("✅ EasySave BMT prêt !");
            saveListManager.DisplaySaves(); // Recharge liste
            
            // GUI prête - boutons liés aux contrôleurs
        }
    }

    // 🚀 NOUVELLE INTERFACE pour GUI (à CÔTÉ de ViewModel)
    public interface IProgressObserverGUI
    {
        void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent);
        void OnBackupComplete(string backupName, double transferTime);
        void OnFileError(string fileName);
        void ShowMessage(string message);
        ObservableCollection<Model_.Save> Saves { get; }
    }
}
