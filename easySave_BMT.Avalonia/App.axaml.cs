using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using easySave_BMT.Avalonia.ViewModels;
using easySave_BMT.Model_;

namespace easySave_BMT.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        private static ThemeVariant ResolveInitialThemeVariant()
        {
            try
            {
                var cfg = Config.Load();
                string pref = (cfg.ThemePreference ?? "auto").Trim().ToLowerInvariant();
                return pref switch
                {
                    "light" => ThemeVariant.Light,
                    "dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
            catch
            {
                return ThemeVariant.Default;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            RequestedThemeVariant = ResolveInitialThemeVariant();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
