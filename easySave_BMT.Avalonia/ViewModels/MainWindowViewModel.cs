using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using easySave_BMT.Avalonia.Services;
using easySave_BMT.ViewModel_;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;

namespace easySave_BMT.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ReactiveObject, IProgressObserverGUI
    {
        private readonly ViewModel _coreViewModel;
        public Window? HostWindow { get; set; }

        // Cache UI: progression par nom de sauvegarde (évite de perdre la progression lors d'un refresh)
        private readonly Dictionary<string, int> _uiProgressBySaveName = new(StringComparer.Ordinal);

        // Auto-clear messages (sauf Dashboard)
        private static readonly TimeSpan MessageAutoClearDelay = TimeSpan.FromSeconds(5);
        private readonly Dictionary<MessageArea, CancellationTokenSource> _messageClearTokens = new();

        // Localization
        public LocalizationService Loc { get; } = new();

        // Interactions (dialogs)
        public Interaction<FolderPickerOpenOptions, string?> BrowseFolderInteraction { get; } = new();
        public Interaction<FilePickerSaveOptions, string?> SaveFileInteraction { get; } = new();

        public MainWindowViewModel()
        {
            // Core
            _coreViewModel = new ViewModel();
            _coreViewModel.guiView = this;
            _coreViewModel.RunAppGUI(this);

            // Commands
            InitCommands();

            // Chargement initial
            LoadConfigValuesFromModel();
            LoadLogs();
            RefreshBackupTypeOptions();
            ListSaves(showUserFeedback: false);

            SetMessageFromCode(100, MessageArea.Dashboard);
            SelectedLogContent = Loc["UiSelectLogFile"];
        }

        private void ShutdownApp()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
