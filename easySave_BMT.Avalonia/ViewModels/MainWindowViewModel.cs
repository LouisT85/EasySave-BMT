using Avalonia.Controls;
using Avalonia.Threading; // Important pour les mises à jour UI depuis un thread
using easySave_BMT.Model_;
using easySave_BMT.ViewModel_; // Votre ViewModel Core
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace easySave_BMT.Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IProgressObserverGUI
    {
        private readonly ViewModel _coreViewModel;
        public Window? HostWindow { get; set; }

        // --- Collections ---
        public ObservableCollection<Model_.Save> Saves { get; } = new();
        public ObservableCollection<string> BackupTypeOptions { get; } = new() { "Complet", "Différentiel" };
        public ObservableCollection<string> LogFiles { get; } = new();

        // --- Sélection & Inputs ---
        private Model_.Save? _selectedSave;
        public Model_.Save? SelectedSave
        {
            get => _selectedSave;
            set => this.RaiseAndSetIfChanged(ref _selectedSave, value);
        }

        private string _newSaveName = string.Empty;
        public string NewSaveName { get => _newSaveName; set => this.RaiseAndSetIfChanged(ref _newSaveName, value); }

        private string _newSaveSourcePath = string.Empty;
        public string NewSaveSourcePath { get => _newSaveSourcePath; set => this.RaiseAndSetIfChanged(ref _newSaveSourcePath, value); }

        private string _newSaveDestinationPath = string.Empty;
        public string NewSaveDestinationPath { get => _newSaveDestinationPath; set => this.RaiseAndSetIfChanged(ref _newSaveDestinationPath, value); }

        private string? _selectedBackupType = "Complet";
        public string? SelectedBackupType { get => _selectedBackupType; set => this.RaiseAndSetIfChanged(ref _selectedBackupType, value); }

        // --- Config ---
        private string _configLogDirectory = string.Empty;
        public string ConfigLogDirectory { get => _configLogDirectory; set => this.RaiseAndSetIfChanged(ref _configLogDirectory, value); }

        private string _configStateFilePath = string.Empty;
        public string ConfigStateFilePath { get => _configStateFilePath; set => this.RaiseAndSetIfChanged(ref _configStateFilePath, value); }

        private string _configLanguage = "fr";
        public string ConfigLanguage { get => _configLanguage; set => this.RaiseAndSetIfChanged(ref _configLanguage, value); }

        // --- Logs ---
        private string _selectedLogFile = string.Empty;
        public string SelectedLogFile
        {
            get => _selectedLogFile;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLogFile, value);
                ViewSelectedLog(); // Auto-load content on selection
            }
        }

        private string _selectedLogContent = "Sélectionnez un fichier pour voir son contenu.";
        public string SelectedLogContent { get => _selectedLogContent; set => this.RaiseAndSetIfChanged(ref _selectedLogContent, value); }

        // --- Status & Progress ---
        private int _progressPercent = 0;
        public int ProgressPercent { get => _progressPercent; set => this.RaiseAndSetIfChanged(ref _progressPercent, value); }

        private string _progressText = "Prêt";
        public string ProgressText { get => _progressText; set => this.RaiseAndSetIfChanged(ref _progressText, value); }

        private string _statusText = "Bienvenue dans EasySave BMT";
        public string StatusText { get => _statusText; set => this.RaiseAndSetIfChanged(ref _statusText, value); }

        // --- Commands ---
        public ReactiveCommand<Unit, Unit> ListCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
        public ReactiveCommand<Unit, Unit> LaunchCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadLogsCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitCommand { get; }

        public MainWindowViewModel()
        {
            // Initialisation du Core
            _coreViewModel = new ViewModel();
            _coreViewModel.guiView = this;
            _coreViewModel.RunAppGUI(this);

            // Commandes
            ListCommand = ReactiveCommand.Create(ListSaves);
            AddCommand = ReactiveCommand.Create(AddSave);
            RemoveCommand = ReactiveCommand.Create(RemoveSave);
            LaunchCommand = ReactiveCommand.CreateFromTask(LaunchBackupAsync);
            LoadConfigCommand = ReactiveCommand.Create(LoadConfigValuesFromModel);
            ConfigCommand = ReactiveCommand.Create(SaveConfigFromViewModel);
            LoadLogsCommand = ReactiveCommand.Create(LoadLogs);
            QuitCommand = ReactiveCommand.Create(() => Environment.Exit(0));

            // Chargement initial
            LoadConfigValuesFromModel();
            LoadLogs();
            ListSaves();
        }

        // --- Logique Métier ---

        private void ListSaves()
        {
            // Rafraîchit la liste interne du Model, la GUI ObservableCollection est liée via le Core si implémenté ainsi, 
            // sinon on doit recharger manuellement si Saves n'est pas la même réf que model.saves.
            // On suppose ici que _coreViewModel.model.saves est la source.
            // Pour être sûr avec Avalonia, on vide et on remplit si nécessaire, ou on utilise DynamicData.
            // Ici, méthode simple :
            _coreViewModel.saveListManager.DisplaySaves();

            Saves.Clear();
            foreach (var s in _coreViewModel.model.saves)
            {
                Saves.Add(s);
            }

            StatusText = $"Liste mise à jour ({Saves.Count} sauvegardes)";
        }

        private void AddSave()
        {
            if (_coreViewModel.model.saves.Count >= 5)
            {
                StatusText = "⚠️ Limite atteinte (5 sauvegardes max).";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewSaveName) || string.IsNullOrWhiteSpace(NewSaveSourcePath) || string.IsNullOrWhiteSpace(NewSaveDestinationPath))
            {
                StatusText = "⚠️ Veuillez remplir tous les champs.";
                return;
            }

            BackupType type = SelectedBackupType == "Complet" ? BackupType.FULL : BackupType.DIFFERENTIAL;
            int res = _coreViewModel.model.AddSave(NewSaveName, NewSaveSourcePath, NewSaveDestinationPath, type);

            if (res == 101) StatusText = "✅ Sauvegarde ajoutée avec succès.";
            else StatusText = "❌ Erreur lors de l'ajout.";

            ListSaves(); // Refresh UI
            // Reset champs
            NewSaveName = ""; NewSaveSourcePath = ""; NewSaveDestinationPath = "";
        }

        private void RemoveSave()
        {
            if (SelectedSave is null) { StatusText = "⚠️ Aucune sauvegarde sélectionnée."; return; }

            int index = _coreViewModel.model.saves.IndexOf(SelectedSave);
            if (index >= 0)
            {
                _coreViewModel.model.RemoveSave(index);
                ListSaves();
                StatusText = "🗑️ Sauvegarde supprimée.";
            }
        }

        private async Task LaunchBackupAsync()
        {
            if (SelectedSave is null) { StatusText = "⚠️ Sélectionnez une sauvegarde à lancer."; return; }

            var saveToRun = SelectedSave;
            StatusText = $"🚀 Lancement de '{saveToRun.name}'...";
            ProgressPercent = 0;

            // Exécution en background pour ne pas geler l'UI
            int result = await Task.Run(() => _coreViewModel.backupLauncher.LaunchBackupType(saveToRun));

            // Post-traitement sur le thread UI
            if (result == 104 || result == 105 || result == 216)
            {
                _coreViewModel.model.FinishBackup(saveToRun);
                StatusText = (result == 216) ? "⚠️ Terminé avec erreurs." : "✅ Backup terminé avec succès.";
            }
            else
            {
                StatusText = $"❌ Erreur critique (Code {result}).";
            }
            ListSaves(); // Pour mettre à jour la date de dernière sauvegarde
        }

        private void LoadConfigValuesFromModel()
        {
            var cfg = _coreViewModel.model.GetConfig();
            ConfigLogDirectory = cfg.LogDirectory;
            ConfigStateFilePath = cfg.StateFilePath;
            ConfigLanguage = cfg.Language;
        }

        private void SaveConfigFromViewModel()
        {
            _coreViewModel.model.UpdateConfig(ConfigLogDirectory, ConfigStateFilePath, ConfigLanguage);
            LoadConfigValuesFromModel();
            StatusText = "💾 Configuration enregistrée.";
        }

        private void LoadLogs()
        {
            LogFiles.Clear();
            if (Directory.Exists(ConfigLogDirectory))
            {
                var files = Directory.GetFiles(ConfigLogDirectory).OrderByDescending(f => f);
                foreach (var f in files) LogFiles.Add(Path.GetFileName(f));
            }
        }

        private void ViewSelectedLog()
        {
            if (string.IsNullOrEmpty(SelectedLogFile)) return;
            string path = Path.Combine(ConfigLogDirectory, SelectedLogFile);
            if (File.Exists(path)) SelectedLogContent = File.ReadAllText(path);
        }

        // --- IProgressObserverGUI Implementation ---
        // Utilisation de Dispatcher.UIThread pour garantir que l'UI se met à jour depuis le thread de backup
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Clamp percent
                ProgressPercent = Math.Clamp(percent, 0, 100);
                ProgressText = $"{backupName} : {percent}% ({filesLeft} fichiers restants)";
            });
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            Dispatcher.UIThread.Post(() => StatusText = $"✅ {backupName} fini en {transferTime:F2}s");
        }

        public void OnFileError(string fileName)
        {
            Dispatcher.UIThread.Post(() => StatusText = $"❌ Erreur fichier: {fileName}");
        }

        public void ShowMessage(string message)
        {
            Dispatcher.UIThread.Post(() => StatusText = message);
        }
    }
}