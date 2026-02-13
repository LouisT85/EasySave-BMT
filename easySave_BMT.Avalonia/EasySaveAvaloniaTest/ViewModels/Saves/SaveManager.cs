using easySave_BMT.Model_;

namespace easySave_BMT.ViewModel_.Saves
{
    public class SaveManager
    {
        private readonly ViewModel _viewModel;

        public SaveManager(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void AddSave()
        {
            if (_viewModel.model.saves.Count < 5)
            {
                string addSaveName = _viewModel.view.SaveName();
                if (addSaveName == "0") return;

                string addSaveSrc = _viewModel.view.SaveSrc();
                if (addSaveSrc == "0") return;

                string addSaveDest = _viewModel.view.SaveDst(addSaveSrc);
                if (addSaveDest == "0") return;

                BackupType backupType = GetBackupType();
                if (backupType == BackupType.NONE) return;

                _viewModel.view.DisplayMessage(_viewModel.model.AddSave(addSaveName, addSaveSrc, addSaveDest, backupType));
            }
            else
            {
                _viewModel.view.DisplayMessage(205);
            }
        }

        public void RemoveSave()
        {
            if (_viewModel.model.saves.Count > 0)
            {
                int choice = _viewModel.view.RemovesaveChoice();
                if (choice == 0) return;

                int index = choice - 1;

                if (index >= 0 && index < _viewModel.model.saves.Count)
                {
                    _viewModel.view.DisplayMessage(_viewModel.model.RemoveSave(index));
                }
                else
                {
                    _viewModel.view.DisplayMessage(206);
                }
            }
            else
            {
                _viewModel.view.DisplayMessage(204);
            }
        }

        private BackupType GetBackupType()
        {
            switch (_viewModel.view.AddSaveBackupType())
            {
                case 0:
                    return BackupType.NONE;
                case 1:
                    return BackupType.FULL;
                case 2:
                    return BackupType.DIFFERENTIAL;
                default:
                    return BackupType.DIFFERENTIAL;
            }
        }
    }
}
