using System;
using easySave_BMT.Resources_;
using easySave_BMT.Model_;

namespace easySave_BMT.View_
{
    public class ConfigurationInputService
    {
        private readonly ValidationService validationService;

        public ConfigurationInputService(ValidationService validationService)
        {
            this.validationService = validationService;
        }

        public void DisplayConfiguration(Config config)
        {
            Console.Clear();
            Console.WriteLine("=== " + ResourceManager.GetString("CurrentConfiguration") + " ===");
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("LogDirectory") + ": " + config.LogDirectory);
            Console.WriteLine(ResourceManager.GetString("StateFile") + ": " + config.StateFilePath);
            Console.WriteLine(ResourceManager.GetString("Language") + ": " + config.Language);
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("PressEnter"));
            Console.ReadLine();
        }

        public string AskForLogDirectory()
        {
            Console.Clear();
            Console.WriteLine("=== " + ResourceManager.GetString("ModifyLogDir") + " ===");
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("LeaveEmptyToKeep"));
            Console.WriteLine("");

            while (true)
            {
                Console.Write(ResourceManager.GetString("NewLogDirectory") + ": ");
                string input = PathFormatter.Rectify(Console.ReadLine());

                if (input == "0")
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                if (validationService.ValidateLogDirectory(input))
                {
                    return input;
                }
            }
        }

        public string AskForStateFilePath(string currentPath)
        {
            Console.Clear();
            Console.WriteLine("=== " + ResourceManager.GetString("ConfigStateFilePath") + " ===");
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("CurrentStateFile") + ": " + currentPath);
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("StateFilePathInstruction"));
            Console.WriteLine(ResourceManager.GetString("LeaveEmptyToKeep"));
            Console.WriteLine("");

            while (true)
            {
                Console.Write(ResourceManager.GetString("NewStateFilePath") + ": ");
                string input = Console.ReadLine();

                if (input == "0" || string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                if (validationService.ValidateStateFilePath(input))
                {
                    return input;
                }
            }
        }

        public string AskForLanguage()
        {
            MenuDisplay menu = new MenuDisplay();
            int choice = menu.ShowLanguageMenu();

            return choice switch
            {
                1 => "fr",
                2 => "en",
                _ => null
            };
        }
    }
}
