using easySave_BMT.Resources_;
using ReactiveUI;

namespace easySave_BMT.Avalonia.Services
{
    public class LocalizationService : ReactiveObject
    {
        public string this[string key] => ResourceManager.GetString(key);

        public void SetLanguage(string language)
        {
            ResourceManager.SetLanguage(language);
            // Indexer bindings refresh on "Item" change.
            this.RaisePropertyChanged("Item");
        }
    }
}
