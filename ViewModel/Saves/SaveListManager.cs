namespace easySave_BMT.ViewModel_.Saves
{
    public class SaveListManager
    {
        private readonly ViewModel _viewModel;

        public SaveListManager(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public int DisplaySaves()
        {
            int reloadResult = _viewModel.model.ReloadSavesFromFile();

            if (reloadResult == 100)
            {
                if (_viewModel.guiView is not null)
                {
                    _viewModel.RefreshGuiSaves();
                    return reloadResult;
                }

                if (_viewModel.model.saves.Count > 0)
                    _viewModel.view.DisplayAllSaves();
                else
                    _viewModel.view.DisplayMessage(204);
            }
            else
            {
                if (_viewModel.guiView is not null)
                {
                    _viewModel.guiView.ShowMessage($"Erreur chargement sauvegardes (code {reloadResult})");
                }
                else
                {
                    _viewModel.view.DisplayMessage(reloadResult);
                }
            }

            return reloadResult;
        }
    }
}
