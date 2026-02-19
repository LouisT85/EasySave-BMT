using easySave_BMT.Model_;
using easySave_BMT.View_;

namespace easySave_BMT.ViewModel_.Saves
{
    /// <summary>
    /// Handles save creation and removal workflows for the presentation layer.
    /// </summary>
    public class SaveManager
    {
        private readonly View _view;
        private readonly Model _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveManager"/> class.
        /// </summary>
        /// <param name="view">The console view adapter.</param>
        /// <param name="model">The domain model facade.</param>
        public SaveManager(View view, Model model)
        {
            _view = view;
            _model = model;
        }

        /// <summary>
        /// Interactively adds a backup save definition.
        /// </summary>
        public void AddSave()
        {
            string addSaveName = _view.SaveName();
            if (addSaveName == "0")
            {
                return;
            }

            string addSaveSrc = _view.SaveSrc();
            if (addSaveSrc == "0")
            {
                return;
            }

            string addSaveDest = _view.SaveDst(addSaveSrc);
            if (addSaveDest == "0")
            {
                return;
            }

            BackupType backupType = GetBackupType();
            if (backupType == BackupType.NONE)
            {
                return;
            }

            _view.DisplayMessage(_model.AddSave(addSaveName, addSaveSrc, addSaveDest, backupType));
        }

        /// <summary>
        /// Interactively removes a backup save definition.
        /// </summary>
        public void RemoveSave()
        {
            if (_model.saves.Count <= 0)
            {
                _view.DisplayMessage(204);
                return;
            }

            int choice = _view.RemovesaveChoice();
            if (choice == 0)
            {
                return;
            }

            int index = choice - 1;
            if (index >= 0 && index < _model.saves.Count)
            {
                _view.DisplayMessage(_model.RemoveSave(index));
                return;
            }

            _view.DisplayMessage(206);
        }

        private BackupType GetBackupType()
        {
            return _view.AddSaveBackupType() switch
            {
                0 => BackupType.NONE,
                1 => BackupType.FULL,
                2 => BackupType.DIFFERENTIAL,
                _ => BackupType.DIFFERENTIAL
            };
        }
    }
}