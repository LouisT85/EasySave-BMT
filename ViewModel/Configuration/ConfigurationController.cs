using easySave_BMT.View_;
using easySave_BMT.Model_;

namespace easySave_BMT.ViewModel_.Configuration
{
    public class ConfigurationController
    {
        private readonly ViewModel _viewModel;

        public ConfigurationController(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void ConfigurationMenu()
        {
            bool inConfigMenu = true;

            while (inConfigMenu)
            {
                int choice = _viewModel.view.ConfigurationMenu();

                switch (choice)
                {
                    case 1:
                        var config = _viewModel.model.GetConfig();
                        _viewModel.view.DisplayCurrentConfiguration(config);
                        break;

                    case 2:
                        string newLogDir = _viewModel.view.AskForLogDirectory();
                        if (!string.IsNullOrWhiteSpace(newLogDir))
                        {
                            _viewModel.model.UpdateConfig(newLogDir, null, null);
                            _viewModel.view.DisplayMessage(218);
                        }
                        break;

                    case 3:
                        string newStatePath = _viewModel.view.AskForStateFilePath();
                        if (!string.IsNullOrWhiteSpace(newStatePath))
                        {
                            _viewModel.model.UpdateConfig(null, newStatePath, null);
                            _viewModel.view.DisplayMessage(218);
                        }
                        break;

                    case 4:
                        string newLang = _viewModel.view.AskForLanguage();
                        if (!string.IsNullOrWhiteSpace(newLang))
                        {
                            _viewModel.model.UpdateConfig(null, null, newLang);
                            _viewModel.view.DisplayMessage(218);
                        }
                        break;

                    case 0:
                        inConfigMenu = false;
                        break;

                    case 5:
                        string newMode = _viewModel.view.AskForLogDestinationMode();
                        if (!string.IsNullOrWhiteSpace(newMode))
                        {
                            string endpointToApply = _viewModel.model.GetConfig().CentralizedLogEndpoint;

                            if (Config.RequiresCentralizedEndpoint(newMode) &&
                                string.IsNullOrWhiteSpace(endpointToApply))
                            {
                                string endpoint = _viewModel.view.AskForCentralizedLogEndpoint();
                                if (string.IsNullOrWhiteSpace(endpoint))
                                {
                                    _viewModel.view.DisplayMessage(206);
                                    break;
                                }

                                endpointToApply = endpoint;
                            }

                            _viewModel.model.UpdateConfig(
                                null,
                                null,
                                null,
                                logDestinationMode: newMode,
                                centralizedLogEndpoint: endpointToApply);

                            _viewModel.view.DisplayMessage(218);
                        }
                        break;

                    case 6:
                        string newEndpoint = _viewModel.view.AskForCentralizedLogEndpoint();
                        if (newEndpoint is not null)
                        {
                            string currentMode = _viewModel.model.GetConfig().LogDestinationMode;
                            if (Config.RequiresCentralizedEndpoint(currentMode) &&
                                string.IsNullOrWhiteSpace(newEndpoint))
                            {
                                _viewModel.view.DisplayMessage(206);
                                break;
                            }

                            _viewModel.model.UpdateConfig(
                                null,
                                null,
                                null,
                                centralizedLogEndpoint: newEndpoint);
                            _viewModel.view.DisplayMessage(218);
                        }
                        break;

                    default:
                        _viewModel.view.DisplayMessage(206);
                        break;
                }
            }
        }
    }
}
