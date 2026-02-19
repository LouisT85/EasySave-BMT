using System;
using easySave_BMT.Model_;
using easySave_BMT.View_;
using easySave_BMT.ViewModel_;

namespace easySave_BMT.ViewModel_.Saves
{
    /// <summary>
    /// Handles save list refresh and display operations.
    /// </summary>
    public class SaveListManager
    {
        private readonly Model _model;
        private readonly View _view;
        private readonly Func<IProgressObserverGUI?> _guiAccessor;
        private readonly Action _refreshGuiSavesAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveListManager"/> class.
        /// </summary>
        /// <param name="model">The domain model facade.</param>
        /// <param name="view">The console view adapter.</param>
        /// <param name="guiAccessor">A delegate returning the current GUI observer.</param>
        /// <param name="refreshGuiSavesAction">A delegate that pushes saves to GUI collections.</param>
        public SaveListManager(
            Model model,
            View view,
            Func<IProgressObserverGUI?> guiAccessor,
            Action refreshGuiSavesAction)
        {
            _model = model;
            _view = view;
            _guiAccessor = guiAccessor;
            _refreshGuiSavesAction = refreshGuiSavesAction;
        }

        /// <summary>
        /// Reloads and displays configured saves for console or GUI usage.
        /// </summary>
        /// <returns>The reload status code.</returns>
        public int DisplaySaves()
        {
            int reloadResult = _model.ReloadSavesFromFile();
            IProgressObserverGUI? guiView = _guiAccessor();

            if (reloadResult == 100)
            {
                if (guiView is not null)
                {
                    _refreshGuiSavesAction();
                    return reloadResult;
                }

                if (_model.saves.Count > 0)
                {
                    _view.DisplayAllSaves();
                }
                else
                {
                    _view.DisplayMessage(204);
                }

                return reloadResult;
            }

            if (guiView is not null)
            {
                guiView.ShowMessage($"Error while loading saves (code {reloadResult}).");
            }
            else
            {
                _view.DisplayMessage(reloadResult);
            }

            return reloadResult;
        }
    }
}