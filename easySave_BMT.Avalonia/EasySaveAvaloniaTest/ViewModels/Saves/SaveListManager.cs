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
                    _viewModel.view.DisplayAllSaves();
                }
                else
                {
                    _viewModel.view.DisplayMessage(204);
                }
            }
            else
            {
                _viewModel.view.DisplayMessage(reloadResult);
            }
        }
    }
}
