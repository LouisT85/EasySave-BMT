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
            Console.WriteLine(ResourceManager.GetString("LogDestinationMode") + ": " + config.LogDestinationMode);
            Console.WriteLine(ResourceManager.GetString("CentralizedLogEndpoint") + ": " +
                              (string.IsNullOrWhiteSpace(config.CentralizedLogEndpoint)
                                  ? ResourceManager.GetString("NotConfigured")
                                  : config.CentralizedLogEndpoint));
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

        public string AskForLogDestinationMode(string currentMode)
        {
            currentMode = Config.NormalizeLogDestinationMode(currentMode);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== " + ResourceManager.GetString("ModifyLogMode") + " ===");
                Console.WriteLine("");
                Console.WriteLine(ResourceManager.GetString("CurrentLogMode") + ": " + currentMode);
                Console.WriteLine("");
                Console.WriteLine("1 - " + ResourceManager.GetString("LogModeLocalOnly"));
                Console.WriteLine("2 - " + ResourceManager.GetString("LogModeCentralizedOnly"));
                Console.WriteLine("3 - " + ResourceManager.GetString("LogModeLocalAndCentralized"));
                Console.WriteLine("");
                Console.WriteLine("0 - " + ResourceManager.GetString("Return"));
                Console.WriteLine("");
                Console.Write("> ");

                string? input = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(input) || input == "0")
                {
                    return null;
                }

                switch (input)
                {
                    case "1":
                        return Config.LogDestinationModeLocalOnly;
                    case "2":
                        return Config.LogDestinationModeCentralizedOnly;
                    case "3":
                        return Config.LogDestinationModeLocalAndCentralized;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ResourceManager.GetString("InvalidOption"));
                        Console.ResetColor();
                        Console.WriteLine(ResourceManager.GetString("PressEnter"));
                        Console.ReadLine();
                        break;
                }
            }
        }

        public string AskForCentralizedLogEndpoint(string currentEndpoint)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== " + ResourceManager.GetString("ModifyCentralizedEndpoint") + " ===");
                Console.WriteLine("");
                Console.WriteLine(ResourceManager.GetString("CurrentCentralizedEndpoint") + ": " +
                                  (string.IsNullOrWhiteSpace(currentEndpoint)
                                      ? ResourceManager.GetString("NotConfigured")
                                      : currentEndpoint));
                Console.WriteLine(ResourceManager.GetString("CentralizedEndpointHint"));
                Console.WriteLine(ResourceManager.GetString("LeaveEmptyToKeep"));
                Console.WriteLine(ResourceManager.GetString("EnterClearToRemove"));
                Console.WriteLine("");
                Console.Write(ResourceManager.GetString("CentralizedLogEndpoint") + ": ");

                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input == "0")
                {
                    return null;
                }

                input = input.Trim();
                if (string.Equals(input, "clear", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Empty;
                }

                if (validationService.ValidateCentralizedLogEndpoint(input))
                {
                    return input;
                }
            }
        }
    }
}
