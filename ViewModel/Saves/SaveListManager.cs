namespace easySave_BMT.ViewModel_.Saves
{
    public class SaveListManager
    {
        private readonly ViewModel _viewModel;

        public SaveListManager(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void DisplaySaves()
        {
            int reloadResult = _viewModel.model.ReloadSavesFromFile();

            if (reloadResult == 100)
            {
                if (_viewModel.model.saves.Count > 0)
                {
                    // Si une vue GUI est présente, on met à jour la liste bindée
                    if (_viewModel.guiView is not null)
                    {
                        _viewModel.RefreshGuiSaves();
                        _viewModel.guiView.ShowMessage($"Liste mise à jour ({_viewModel.model.saves.Count} sauvegardes)");
                    }
                    else
                    {
                        // Mode console historique
                        _viewModel.view.DisplayAllSaves();
                    }
                }
                else
                {
                    if (_viewModel.guiView is not null)
                    {
                        _viewModel.guiView.ShowMessage("Aucune sauvegarde définie.");
                    }
                    else
                    {
                        _viewModel.view.DisplayMessage(204);
                    }
                }
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
        }
    }
}
