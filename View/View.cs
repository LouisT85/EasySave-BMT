using System;
using System.IO;
using easySave_BMT.ViewModel_;
using easySave_BMT.Model_;
using easySave_BMT.Resources_;

namespace easySave_BMT.View_
{
    public class View
    {
        private ViewModel viewModel;

        public View(ViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        private int InteractiveMenu(string title, string[] items, bool includeReturn = true)
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
                        Console.WriteLine("  0 - " + ResourceManager.GetString("Return"));
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

        public int Menu()
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
                    Console.WriteLine("  6 - " + ResourceManager.GetString("Quit"));
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

        public int ConfigurationMenu()
        {
            string[] configItems = {
                "1 - " + ResourceManager.GetString("DisplayConfig"),
                "2 - " + ResourceManager.GetString("ModifyLogDir"),
                "3 - " + ResourceManager.GetString("ModifyStateFile"),
                "4 - " + ResourceManager.GetString("ChangeLanguage")
            };

            return InteractiveMenu(ResourceManager.GetString("Configuration"), configItems);
        }

        public void DisplayCurrentConfiguration(Config config)
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

        private bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            try
            {
                Path.GetFullPath(path);

                char[] invalidChars = Path.GetInvalidPathChars();
                foreach (char c in invalidChars)
                {
                    if (path.Contains(c)) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckLogDirectory(string path)
        {
            if (path == "0") return true;

            if (!IsValidPath(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            try
            {
                Directory.CreateDirectory(path);

                string testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("NoWritePermission"));
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("Error") + ": " + ex.Message);
                Console.ResetColor();
                return false;
            }
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
                string input = RectifyPath(Console.ReadLine());

                if (input == "0")
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    return null;
                }

                if (CheckLogDirectory(input))
                {
                    return input;
                }
            }
        }

        private bool CheckStateFilePath(string path)
        {
            if (path == "0") return true;

            if (!path.Contains(".") || !path.EndsWith(".json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("StateFilePathWarning"));
                Console.ResetColor();
                return false;
            }

            if (!IsValidPath(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);

                string testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("NoWritePermission"));
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("Error") + ": " + ex.Message);
                Console.ResetColor();
                return false;
            }
        }

        public string AskForStateFilePath()
        {
            Console.Clear();
            Console.WriteLine("=== " + ResourceManager.GetString("ConfigStateFilePath") + " ===");
            Console.WriteLine("");
            Console.WriteLine(ResourceManager.GetString("CurrentStateFile") + ": " + viewModel.model.GetConfig().StateFilePath);
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

                if (CheckStateFilePath(input))
                {
                    return input;
                }
            }
        }

        public string AskForLanguage()
        {
            string[] langItems = {
                "1 - Français (fr)",
                "2 - English (en)"
            };

            int choice = InteractiveMenu(ResourceManager.GetString("ChangeLanguage"), langItems);

            return choice switch
            {
                1 => "fr",
                2 => "en",
                _ => null
            };
        }

        public void DisplayMessage(int id)
        {
            if (id == 218)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n" + ResourceManager.GetString("ConfigUpdated"));
                Console.WriteLine(ResourceManager.GetString("PressEnter"));
                Console.ReadLine();
                Console.ResetColor();
                return;
            }

            if (id < 100)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                switch (id)
                {
                    case 1:
                        Console.WriteLine("\n" + ResourceManager.GetString("PressEnterToMenu"));
                        Console.ReadLine();
                        break;

                    case 2:
                        Console.WriteLine("\n(Entrez 0 pour revenir au menu)");
                        break;

                    case 3:
                        Console.Clear();
                        Console.WriteLine("\nBackup information :");
                        break;

                    case 4:
                        Console.WriteLine("\n" + ResourceManager.GetString("PressEnterMore"));
                        Console.ReadLine();
                        break;
                }
            }
            else if (id < 200)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                switch (id)
                {
                    case 100:
                        Console.WriteLine("\n########################### EASYSAVE BMT ########################");
                        DisplayMessage(1);
                        break;

                    case 101:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileAddedSuccess"));
                        DisplayMessage(1);
                        break;

                    case 102:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileSavedSuccess"));
                        break;

                    case 103:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileDeletedSuccess"));
                        DisplayMessage(1);
                        break;

                    case 104:
                        Console.WriteLine("\n" + ResourceManager.GetString("BackupSuccess"));
                        break;

                    case 105:
                        Console.WriteLine("\n" + ResourceManager.GetString("NoChanges"));
                        break;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                switch (id)
                {
                    case 200:
                        Console.WriteLine("\n" + ResourceManager.GetString("RestoreJSON"));
                        DisplayMessage(1);
                        break;

                    case 201:
                        Console.WriteLine("\n" + ResourceManager.GetString("AddFailed"));
                        DisplayMessage(1);
                        break;

                    case 202:
                        Console.WriteLine("\n" + ResourceManager.GetString("SaveFailed"));
                        DisplayMessage(1);
                        break;

                    case 203:
                        Console.WriteLine("\n" + ResourceManager.GetString("DeleteFailed"));
                        DisplayMessage(1);
                        break;

                    case 204:
                        Console.WriteLine("\n" + ResourceManager.GetString("ListEmpty"));
                        DisplayMessage(1);
                        break;

                    case 205:
                        Console.WriteLine("\n" + ResourceManager.GetString("ListFull"));
                        DisplayMessage(1);
                        break;

                    case 206:
                        Console.WriteLine("\n" + ResourceManager.GetString("InvalidOption"));
                        break;

                    case 207:
                        Console.WriteLine("\n" + ResourceManager.GetString("TransferFailed"));
                        break;

                    case 208:
                        Console.WriteLine("\n" + ResourceManager.GetString("BackupTypeNotExist"));
                        break;

                    case 209:
                        Console.WriteLine("\n" + ResourceManager.GetString("CopyFailed"));
                        DisplayMessage(1);
                        break;

                    case 210:
                        Console.WriteLine("\n" + ResourceManager.GetString("CreateFolderFailed"));
                        DisplayMessage(1);
                        break;

                    case 211:
                        Console.WriteLine("\n" + ResourceManager.GetString("DirectoryNotExist"));
                        break;

                    case 212:
                        Console.WriteLine("\n" + ResourceManager.GetString("ChooseDifferentPath"));
                        break;

                    case 213:
                        Console.WriteLine("\n" + ResourceManager.GetString("DestinationNotExist"));
                        break;

                    case 214:
                        Console.WriteLine("\n" + ResourceManager.GetString("NameTaken"));
                        break;

                    case 215:
                        Console.WriteLine("\n" + ResourceManager.GetString("EnterValidName"));
                        break;

                    case 216:
                        Console.WriteLine("\n" + ResourceManager.GetString("BackupCompletedWithErrors"));
                        break;

                    case 217:
                        Console.WriteLine("\n" + ResourceManager.GetString("DestinationInsideSource"));
                        break;

                    default:
                        Console.WriteLine("\n" + ResourceManager.GetString("UnknownError"));
                        DisplayMessage(1);
                        break;
                }
            }
            Console.ResetColor();
        }

        private static bool CheckInt(string input)
        {
            try
            {
                int.Parse(input);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int AddSaveBackupType()
        {
            string[] backupTypes = {
                "1 - " + ResourceManager.GetString("FullBackup"),
                "2 - " + ResourceManager.GetString("DifferentialBackup")
            };

            return InteractiveMenu(ResourceManager.GetString("BackupType"), backupTypes);
        }

        private bool CheckName(string name)
        {
            int length = name.Length;
            if(length >= 1 && length <= 20)
            {
                if(!this.viewModel.model.saves.Exists(save => save.name == name))
                {
                    return true;
                }
                DisplayMessage(214);
                return false;
            }
            DisplayMessage(215);
            return false;
        }

        private void SavesJobReport(int shift)
        {
            var saves = this.viewModel.model.saves;

            for (int i =0; i<saves.Count; i++)
            {
                Console.WriteLine(
                    "\n" + (i + shift) + " - " + ResourceManager.GetString("Name") + ": " + saves[i].name
                    + "\n      " + ResourceManager.GetString("Source") + ": " + saves[i].src
                    + "\n      " + ResourceManager.GetString("Destination") + ": " + saves[i].dst
                    + "\n      " + ResourceManager.GetString("Type") + ": " + saves[i].backupType
                );
            }
        }

        public void DisplayAllSaves()
        {
            Console.Clear();
            Console.WriteLine(ResourceManager.GetString("BackupList") + " : ");
            SavesJobReport(1);
            DisplayMessage(1);
        }

        public string SaveName()
        {
            Console.Clear();
            Console.WriteLine(ResourceManager.GetString("BackupSettings"));
            DisplayMessage(2);

            Console.WriteLine("\n" + ResourceManager.GetString("EnterName"));
            string name = Console.ReadLine();

            while (!CheckName(name))
            {
                name = Console.ReadLine();
            }
            return name;
        }

        private string RectifyPath(string path)
        {
            if(path != "0" && path.Length >= 1)
            {
                path += (path.EndsWith("/") || path.EndsWith("\\")) ? "" : "\\";
                path = path.Replace("/", "\\");
            }
            return path.ToLower();
        }

        public string SaveSrc()
        {
            Console.WriteLine("\n" + ResourceManager.GetString("EnterSourceDirectory"));
            string src = RectifyPath(Console.ReadLine());

            while(!Directory.Exists(src) && src != "0")
            {
                DisplayMessage(211);
                src = RectifyPath(Console.ReadLine());
            }
            return src;
        }

        public bool ChecksaveDst(string src, string dst)
        {
            if(dst == "0")
            {
                return true;
            }
            else if (Directory.Exists(dst))
            {
                if(src != dst)
                {
                    if(dst.Length > src.Length)
                    {
                        if(src != dst.Substring(0, src.Length))
                        {
                            return true;
                        }
                        else
                        {
                            DisplayMessage(217);
                            return false;
                        }
                    }
                    return true;
                }
                DisplayMessage(212);
                return false;
            }
            DisplayMessage(213);
            return false;
        }

        public string SaveDst(string src)
        {
            Console.WriteLine("\n" + ResourceManager.GetString("EnterDestinationDirectory"));
            string dst = RectifyPath(Console.ReadLine());

            while (!ChecksaveDst(src, dst))
            {
                dst= RectifyPath(Console.ReadLine());
            }
            return dst;
        }

        private string DisplaySize(long octet)
        {
            if(octet > 1000000000000)
            {
                return Math.Round((decimal)octet / 1000000000000, 2)+ "To";
            }else if(octet > 1000000000)
            {
                return Math.Round((decimal)octet / 1000000000, 2) + "Go";
            }else if(octet > 1000000)
            {
                return Math.Round((decimal)octet / 1000000, 2)+ "Mo";
            }
            else if(octet > 1000)
            {
                return Math.Round((decimal)octet / 1000, 2) + "ko";
            }
            else
            {
                return octet + "o";
            }
        }

        private void DisplayProgressBar(int percent)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(ResourceManager.GetString("Progress") + ": [ " + percent + " %]");
            Console.ResetColor();

            Console.Write(" [");
            for (int i = 0; i < 100; i += 5)
            {
                if (percent > i)
                {
                    Console.Write("#");
                }
                else
                {
                    Console.Write(".");
                }
            }
            Console.Write("]\n\n");
        }

        public void DisplayCurrentState(string name, int fileLeft, long leftSize, long curSize, int percent)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Backup: {name}");
            Console.WriteLine($"{ResourceManager.GetString("CurrentFile")}: {DisplaySize(curSize)}");
            Console.WriteLine($"{ResourceManager.GetString("FilesRemaining")}: {fileLeft}");
            Console.WriteLine($"{ResourceManager.GetString("SizeRemaining")}: {DisplaySize(leftSize)}");
            DisplayProgressBar(percent);
        }

        public void DisplayBackupRecap(string name, double transfertTime)
        {
            Console.WriteLine("\n\n" +
                "Backup : " + name + " " + ResourceManager.GetString("Completed") + "\n"
                +"\n" + ResourceManager.GetString("Duration") + " : " + transfertTime + "ms\n"
            );
            DisplayProgressBar(100);
        }

        public void DisplayFiledError(string name)
        {
            Console.WriteLine(ResourceManager.GetString("FailedForFile") + " " + name);
        }

        public int RemovesaveChoice()
        {
            var saves = viewModel.model.saves;
            if (saves.Count == 0)
            {
                DisplayMessage(204);
                return 0;
            }

            string[] items = new string[saves.Count];
            for (int i = 0; i < saves.Count; i++)
                items[i] = $"{i + 1} - {saves[i].name}";

            return InteractiveMenu(ResourceManager.GetString("DeleteBackup"), items);
        }

        public int LaunchBackupChoice()
        {
            var saves = viewModel.model.saves;
            string[] items = new string[saves.Count + 1];
            items[0] = "1 - " + ResourceManager.GetString("BackupAll");
            for (int i = 0; i < saves.Count; i++)
                items[i + 1] = $"{i + 2} - {saves[i].name}";

            return InteractiveMenu(ResourceManager.GetString("LaunchBackup"), items);
        }
    }
}
