using Avalonia.Controls;
using easySave_BMT.ViewModel_;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace easySave_BMT.Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IProgressObserverGUI
    {
        private readonly ViewModel _coreViewModel;
        
        public ObservableCollection<Model_.Save> Saves { get; } = new();

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
        public ReactiveCommand<Unit, Unit> ConfigCommand { get; }
        public ReactiveCommand<Unit, Unit> QuitCommand { get; }

        public MainWindowViewModel()
        {
            _coreViewModel = new ViewModel();
            _coreViewModel.guiView = this;
            _coreViewModel.RunAppGUI(this);
            
            ListCommand = ReactiveCommand.Create(ListSaves);
            AddCommand = ReactiveCommand.Create(AddSave);
            RemoveCommand = ReactiveCommand.Create(RemoveSave);
            LaunchCommand = ReactiveCommand.Create(LaunchBackup);
            ConfigCommand = ReactiveCommand.Create(ConfigMenu);
            QuitCommand = ReactiveCommand.Create(Quit);
        }

        private void ListSaves() 
        {
            _coreViewModel.saveListManager.DisplaySaves();
            StatusText = $"Liste mise à jour ({_coreViewModel.model.saves.Count} saves)";
        }

        private void AddSave() 
        {
            _coreViewModel.saveManager.AddSave();
            StatusText = "Save ajouté (console pour l'instant)";
        }

        private void RemoveSave() 
        {
            _coreViewModel.saveManager.RemoveSave();
            StatusText = "Save supprimé (console pour l'instant)";
        }

        private void LaunchBackup() 
        {
            StatusText = "🚀 Lancement backup...";
            _coreViewModel.backupLauncher.LaunchBackupsave();
        }

        private void ConfigMenu() 
        {
            _coreViewModel.configController.ConfigurationMenu();
            StatusText = "Configuration (console pour l'instant)";
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
