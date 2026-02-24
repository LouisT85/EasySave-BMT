using System;
using easySave_BMT.Resources_;

namespace easySave_BMT.View_
{
    public class MenuDisplay
    {
        public int ShowInteractiveMenu(string title, string[] items, bool includeReturn = true)
        {
            int selectedIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== " + title + " ===\n");

                for (int i = 0; i < items.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"> {items[i]}");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"  {items[i]}");
                    }
                }

                if (includeReturn)
                {
                    Console.WriteLine("");
                    if (selectedIndex == items.Length)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("> 0 - " + ResourceManager.GetString("Return"));
                        Console.ResetColor();
                    }
                    else
                        Console.WriteLine(" 0 - " + ResourceManager.GetString("Return"));
                }

                Console.WriteLine("\n" + ResourceManager.GetString("MenuNavigation"));
                var key = Console.ReadKey(true);

                if (char.IsDigit(key.KeyChar))
                {
                    int choice = key.KeyChar - '0';
                    if (choice == 0 && includeReturn) return 0;
                    if (choice >= 1 && choice <= items.Length) return choice;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex == 0) ? items.Length : selectedIndex - 1;
                        break;

                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex == items.Length) ? 0 : selectedIndex + 1;
                        break;

                    case ConsoleKey.Enter:
                        return (includeReturn && selectedIndex == items.Length) ? 0 : selectedIndex + 1;

                    case ConsoleKey.Escape:
                        return 0;
                }
            }
        }

        public int ShowMainMenu()
        {
            string[] menuItems = {
                "1 - " + ResourceManager.GetString("DisplayBackups"),
                "2 - " + ResourceManager.GetString("AddBackup"),
                "3 - " + ResourceManager.GetString("DeleteBackup"),
                "4 - " + ResourceManager.GetString("RunBackup"),
                "5 - " + ResourceManager.GetString("Configuration")
            };

            int selectedIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Easy Save - BMT ===");
                Console.WriteLine("");

                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("> " + menuItems[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("  " + menuItems[i]);
                    }
                }

                Console.WriteLine("");

                if (selectedIndex == 5)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("> 6 - " + ResourceManager.GetString("Quit"));
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(" 6 - " + ResourceManager.GetString("Quit"));
                }

                Console.WriteLine("");
                Console.WriteLine(ResourceManager.GetString("MenuNavigation"));

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (char.IsDigit(key.KeyChar))
                {
                    int choice = key.KeyChar - '0';
                    if (choice >= 1 && choice <= 6) return choice;
                }

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex == 0) ? 5 : selectedIndex - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex == 5) ? 0 : selectedIndex + 1;
                        break;
                    case ConsoleKey.Enter:
                        return (selectedIndex == 5) ? 6 : selectedIndex + 1;
                    case ConsoleKey.Escape:
                        return 6;
                }
            }
        }

        public int ShowConfigurationMenu()
        {
            string[] configItems = {
                "1 - " + ResourceManager.GetString("DisplayConfig"),
                "2 - " + ResourceManager.GetString("ModifyLogDir"),
                "3 - " + ResourceManager.GetString("ModifyStateFile"),
                "4 - " + ResourceManager.GetString("ChangeLanguage"),
                "5 - " + ResourceManager.GetString("ModifyLogMode"),
                "6 - " + ResourceManager.GetString("ModifyCentralizedEndpoint")
            };

            return ShowInteractiveMenu(ResourceManager.GetString("Configuration"), configItems);
        }

        public int ShowBackupTypeMenu()
        {
            string[] backupTypes = {
                "1 - " + ResourceManager.GetString("FullBackup"),
                "2 - " + ResourceManager.GetString("DifferentialBackup")
            };

            return ShowInteractiveMenu(ResourceManager.GetString("BackupType"), backupTypes);
        }

        public int ShowLanguageMenu()
        {
            string[] langItems = {
                "1 - Français (fr)",
                "2 - English (en)"
            };

            int choice = ShowInteractiveMenu(ResourceManager.GetString("ChangeLanguage"), langItems);

            return choice switch
            {
                1 => 1,
                2 => 2,
                _ => 0
            };
        }
    }
}
