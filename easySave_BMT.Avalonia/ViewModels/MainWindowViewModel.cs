using Avalonia.Controls;
using easySave_BMT.Model_;
using easySave_BMT.ViewModel_;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace easySave_BMT.Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IProgressObserverGUI
    {
        private readonly ViewModel _coreViewModel;

        // Référence à la fenêtre hôte pour les boîtes de dialogue
        public Window? HostWindow { get; set; }

        // Liste bindée dans la GUI
        public ObservableCollection<Model_.Save> Saves { get; } = new();

        // Sauvegarde sélectionnée dans la liste
        private Model_.Save? _selectedSave;
        public Model_.Save? SelectedSave
        {
            get => _selectedSave;
            set => this.RaiseAndSetIfChanged(ref _selectedSave, value);
        }

        // Champs pour la création de sauvegarde
        private string _newSaveName = string.Empty;
        public string NewSaveName
        {
            get => _newSaveName;
            set => this.RaiseAndSetIfChanged(ref _newSaveName, value);
        }

        private string _newSaveSourcePath = string.Empty;
        public string NewSaveSourcePath
        {
            get => _newSaveSourcePath;
            set => this.RaiseAndSetIfChanged(ref _newSaveSourcePath, value);
        }

        private string _newSaveDestinationPath = string.Empty;
        public string NewSaveDestinationPath
        {
            get => _newSaveDestinationPath;
            set => this.RaiseAndSetIfChanged(ref _newSaveDestinationPath, value);
        }

        private string? _selectedBackupType;
        public string? SelectedBackupType
        {
            get => _selectedBackupType;
            set => this.RaiseAndSetIfChanged(ref _selectedBackupType, value);
        }

        // Champs de configuration (bindés à Config)
        private string _configLogDirectory = string.Empty;
        public string ConfigLogDirectory
        {
            get => _configLogDirectory;
            set => this.RaiseAndSetIfChanged(ref _configLogDirectory, value);
        }

        private string _configStateFilePath = string.Empty;
        public string ConfigStateFilePath
        {
            get => _configStateFilePath;
            set => this.RaiseAndSetIfChanged(ref _configStateFilePath, value);
        }

        private string _configLanguage = string.Empty;
        public string ConfigLanguage
        {
            get => _configLanguage;
            set => this.RaiseAndSetIfChanged(ref _configLanguage, value);
        }

        private int _progressPercent = 0;
        public int ProgressPercent
        {
            get => _progressPercent;
            set => this.RaiseAndSetIfChanged(ref _progressPercent, value);
        }

        private string _progressText = "Prêt";
        public string ProgressText
        {
            get => _progressText;
            set => this.RaiseAndSetIfChanged(ref _progressText, value);
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set => this.RaiseAndSetIfChanged(ref _statusText, value);
        }

        public ReactiveCommand<Unit, Unit> ListCommand { get; }
        public ReactiveCommand<Unit, Unit> AddCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
        public ReactiveCommand<Unit, Unit> LaunchCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitCommand { get; }

        // Visibilité du panneau de configuration
        private bool _isConfigVisible = false;
        public bool IsConfigVisible
        {
            get => _isConfigVisible;
            set => this.RaiseAndSetIfChanged(ref _isConfigVisible, value);
        }

        public MainWindowViewModel()
        {
            _coreViewModel = new ViewModel();
            _coreViewModel.guiView = this;
            _coreViewModel.RunAppGUI(this);

            ListCommand = ReactiveCommand.Create(ListSaves);
            AddCommand = ReactiveCommand.Create(AddSave);
            RemoveCommand = ReactiveCommand.Create(RemoveSave);
            LaunchCommand = ReactiveCommand.Create(LaunchBackup);
            ToggleConfigCommand = ReactiveCommand.Create<Unit>(_ =>
            {
                IsConfigVisible = !IsConfigVisible;
            });
            LoadConfigCommand = ReactiveCommand.Create(LoadConfigValuesFromModel);
            ConfigCommand = ReactiveCommand.Create(SaveConfigFromViewModel);
            QuitCommand = ReactiveCommand.Create(Quit);

            // Charger la configuration au démarrage
            LoadConfigValuesFromModel();
        }

        private void ListSaves()
        {
            _coreViewModel.saveListManager.DisplaySaves();
            StatusText = $"Liste mise à jour ({_coreViewModel.model.saves.Count} sauvegardes)";
        }

        private void AddSave()
        {
            if (_coreViewModel.model.saves.Count >= 5)
            {
                StatusText = "Nombre maximal de sauvegardes atteint (5).";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewSaveName) ||
                string.IsNullOrWhiteSpace(NewSaveSourcePath) ||
                string.IsNullOrWhiteSpace(NewSaveDestinationPath) ||
                string.IsNullOrWhiteSpace(SelectedBackupType))
            {
                StatusText = "Veuillez renseigner tous les champs de la nouvelle sauvegarde.";
                return;
            }

            BackupType backupType = SelectedBackupType.Contains("Complet", StringComparison.OrdinalIgnoreCase)
                ? BackupType.FULL
                : BackupType.DIFFERENTIAL;

            int result = _coreViewModel.model.AddSave(NewSaveName, NewSaveSourcePath, NewSaveDestinationPath, backupType);

            if (result == 101)
            {
                StatusText = "Sauvegarde ajoutée.";
            }
            else
            {
                StatusText = "Erreur lors de l'ajout de la sauvegarde.";
            }

            _coreViewModel.saveListManager.DisplaySaves();
        }

        private void RemoveSave()
        {
            if (SelectedSave is null)
            {
                StatusText = "Sélectionnez une sauvegarde à supprimer.";
                return;
            }

            int index = _coreViewModel.model.saves.IndexOf(SelectedSave);
            if (index < 0)
            {
                StatusText = "Sélection invalide.";
                return;
            }

            int result = _coreViewModel.model.RemoveSave(index);
            _coreViewModel.saveListManager.DisplaySaves();

            if (result == 103)
            {
                StatusText = "Sauvegarde supprimée.";
            }
            else
            {
                StatusText = "Erreur lors de la suppression de la sauvegarde.";
            }
        }

        private void LaunchBackup()
        {
            if (SelectedSave is null)
            {
                StatusText = "Sélectionnez une sauvegarde à lancer.";
                return;
            }

            StatusText = $"🚀 Lancement backup '{SelectedSave.name}'...";

            int result = _coreViewModel.backupLauncher.LaunchBackupType(SelectedSave);

            if (result == 104 || result == 105 || result == 216)
            {
                _coreViewModel.model.FinishBackup(SelectedSave);
            }

            _coreViewModel.saveListManager.DisplaySaves();

            if (result == 104 || result == 105)
            {
                StatusText = "Backup terminée.";
            }
            else if (result == 216)
            {
                StatusText = "Backup terminée avec erreurs.";
            }
            else
            {
                StatusText = $"Erreur lors du backup (code {result}).";
            }
        }

        private void LoadConfigValuesFromModel()
        {
            var cfg = _coreViewModel.model.GetConfig();
            ConfigLogDirectory = cfg.LogDirectory;
            ConfigStateFilePath = cfg.StateFilePath;
            ConfigLanguage = cfg.Language;
            StatusText = "Configuration chargée.";
        }

        private void SaveConfigFromViewModel()
        {
            // Met à jour la configuration à partir des champs GUI
            _coreViewModel.model.UpdateConfig(ConfigLogDirectory, ConfigStateFilePath, ConfigLanguage);
            // Recharge les valeurs normalisées depuis le modèle (chemins éventuellement ajustés)
            LoadConfigValuesFromModel();
            _coreViewModel.saveListManager.DisplaySaves();
            StatusText = "Configuration enregistrée.";
        }

        private void Quit() => Environment.Exit(0);

        // Implémentation IProgressObserverGUI
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            ProgressPercent = percent;
            ProgressText = $"{backupName}: {percent}% ({filesLeft} fichiers)";
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            StatusText = $"✅ {backupName} terminé ({transferTime:F1}s)";
        }

        public void OnFileError(string fileName)
        {
            StatusText = $"❌ Erreur: {fileName}";
        }

        public void ShowMessage(string message)
        {
            StatusText = message;
        }
    }
}
