using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySaveAvaloniaTest.ViewModel;  // Ton namespace

namespace EasySaveAvaloniaTest.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IProgressObserverGUI
    {
        [ObservableProperty] private string statusText = "EasySave prêt !";
        [ObservableProperty] private int progressPercent;
        [ObservableProperty] private ObservableCollection<Model.Save> saves = new();

        private readonly ViewModel coreVM = new();  // Ta ViewModel console

        public MainWindowViewModel()
        {
            coreVM.guiView = this;
            coreVM.RunAppGUI(this);  // Initialise logs/saves
        }

        [RelayCommand]
        private void ListerSaves()
        {
            coreVM.saveListManager.DisplaySaves();
            Saves = coreVM.Saves;  // Bind ta ObservableCollection
            StatusText = $"Liste mise à jour ({Saves.Count} saves)";
        }

        [RelayCommand]
        private void AjouterSave() => coreVM.saveManager.AddSave();

        [RelayCommand]
        private void SupprimerSave() => coreVM.saveManager.RemoveSave();

        [RelayCommand]
        private void LancerBackup() => coreVM.backupLauncher.LaunchBackup();

        [RelayCommand]
        private void Config() => coreVM.configController.ConfigurationMenu();

        // Implémente IProgressObserverGUI (ta ViewModel l'appelle)
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            ProgressPercent = percent;
            StatusText = $"{backupName}: {percent}% ({filesLeft} fichiers)";
        }

        public void OnBackupComplete(string backupName, double transferTime) => StatusText = $"{backupName} fini ({transferTime:F1}s)";
        public void OnFileError(string fileName) => StatusText = $"Erreur: {fileName}";
        public void ShowMessage(string message) => StatusText = message;
    }
}
