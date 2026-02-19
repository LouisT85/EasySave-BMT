using easySave_BMT.Model_;
using easySave_BMT.View_;

namespace easySave_BMT.ViewModel_.Configuration
{
    /// <summary>
    /// Handles configuration-related user interactions.
    /// </summary>
    public class ConfigurationController
    {
        private readonly View _view;
        private readonly Model _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
        /// </summary>
        /// <param name="view">The console view adapter.</param>
        /// <param name="model">The domain model facade.</param>
        public ConfigurationController(View view, Model model)
        {
            _view = view;
            _model = model;
        }

        /// <summary>
        /// Runs the interactive configuration menu loop.
        /// </summary>
        public void ConfigurationMenu()
        {
            bool inConfigMenu = true;

            while (inConfigMenu)
            {
                int choice = _view.ConfigurationMenu();

                switch (choice)
                {
                    case 1:
                        var config = _model.GetConfig();
                        _view.DisplayCurrentConfiguration(config);
                        break;

                    case 2:
                        string newLogDir = _view.AskForLogDirectory();
                        if (!string.IsNullOrWhiteSpace(newLogDir))
                        {
                            _model.UpdateConfig(newLogDir, null, null);
                            _view.DisplayMessage(218);
                        }
                        break;

                    case 3:
                        string newStatePath = _view.AskForStateFilePath();
                        if (!string.IsNullOrWhiteSpace(newStatePath))
                        {
                            _model.UpdateConfig(null, newStatePath, null);
                            _view.DisplayMessage(218);
                        }
                        break;

                    case 4:
                        string newLang = _view.AskForLanguage();
                        if (!string.IsNullOrWhiteSpace(newLang))
                        {
                            _model.UpdateConfig(null, null, newLang);
                            _view.DisplayMessage(218);
                        }
                        break;

                    case 0:
                        inConfigMenu = false;
                        break;

                    default:
                        _view.DisplayMessage(206);
                        break;
                }
            }
        }
    }
}