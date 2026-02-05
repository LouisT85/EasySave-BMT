using System;
using System.IO;
using easySave_BMT.ViewModel_;
using easySave_BMT.Model_;
using easySave_BMT.Resources_;


namespace easySave_BMT.View_
{
    /// <summary>
    /// This class handle the all console interface for our EasySave application.
    /// It create interactive menu and display information to user in nice way.
    /// </summary>
    public class View
    {
        /// <summary>
        /// The reference to ViewModel, use to access model data.
        /// </summary>
        private ViewModel viewModel;


        /// <summary>
        /// Constructor, initialize the view with viewModel instance.
        /// </summary>
        /// <param name="viewModel">The viewmodel that connect to model.</param>
        public View(ViewModel viewModel)
        {
            this.viewModel = viewModel;
        }


        /// <summary>
        /// Display a generic interactive menu with arrow keys navigation.
        /// User can select item with up/down arrows, enter or digit keys.
        /// </summary>
        /// <param name="title">The title show at top of menu.</param>
        /// <param name="items">Array of menu items strings.</param>
        /// <param name="includeReturn">If true, add return option (0).</param>
        /// <returns>The selected choice index, start from 1, 0 for return.</returns>
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


        /// <summary>
        /// Show the main menu of EasySave with all options.
        /// Support keyboard navigation and return choice number.
        /// </summary>
        /// <returns>Number from 1 to 6 for each menu option.</returns>
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


        /// <summary>
        /// Display configuration submenu using InteractiveMenu.
        /// </summary>
        /// <returns>Choice from 1 to 4.</returns>
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


        /// <summary>
        /// Show current configuration values like log dir, state file, language.
        /// Wait for user enter to continue.
        /// </summary>
        /// <param name="config">The config object to display.</param>
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


        /// <summary>
        /// Check if a path is valid, no invalid chars and can resolve.
        /// </summary>
        /// <param name="path">Path string to validate.</param>
        /// <returns>True if valid path.</returns>
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


        /// <summary>
        /// Validate log directory path, test create and write permission.
        /// "0" mean keep current.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if valid and writable.</returns>
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


        /// <summary>
        /// Ask user for new log directory, validate it.
        /// Return null to keep current.
        /// </summary>
        /// <returns>New path or null.</returns>
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


        /// <summary>
        /// Check state file path, must end .json and directory writable.
        /// "0" mean keep current.
        /// </summary>
        /// <param name="path">Path to validate.</param>
        /// <returns>True if valid.</returns>
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


        /// <summary>
        /// Prompt for new state file path with current shown.
        /// </summary>
        /// <returns>New path or null to keep.</returns>
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


        /// <summary>
        /// Show language choice menu, return code or null.
        /// </summary>
        /// <returns>"fr", "en" or null.</returns>
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


        /// <summary>
        /// Display message base on id code, with color and wait enter if needed.
        /// Id categorize success, error, info.
        /// </summary>
        /// <param name="id">Message id number.</param>
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


        /// <summary>
        /// Simple check if string is parsable to int.
        /// </summary>
        /// <param name="input">String to test.</param>
        /// <returns>True if integer.</returns>
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


        /// <summary>
        /// Menu for choose backup type full or differential.
        /// </summary>
        /// <returns>1 for full, 2 for differential.</returns>
        public int AddSaveBackupType()
        {
            string[] backupTypes = {
                "1 - " + ResourceManager.GetString("FullBackup"),
                "2 - " + ResourceManager.GetString("DifferentialBackup")
            };


            return InteractiveMenu(ResourceManager.GetString("BackupType"), backupTypes);
        }


        /// <summary>
        /// Validate backup name, length 1-20, unique.
        /// </summary>
        /// <param name="name">Name to check.</param>
        /// <returns>True if valid.</returns>
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


        /// <summary>
        /// Print list of saves with details, start numbering from shift.
        /// </summary>
        /// <param name="shift">Number offset for display.</param>
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


        /// <summary>
        /// Clear screen and display all saves list.
        /// </summary>
        public void DisplayAllSaves()
        {
            Console.Clear();
            Console.WriteLine(ResourceManager.GetString("BackupList") + " : ");
            SavesJobReport(1);
            DisplayMessage(1);
        }


        /// <summary>
        /// Ask for backup name, validate it loop until good.
        /// </summary>
        /// <returns>Valid name string.</returns>
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


        /// <summary>
        /// Normalize path, add backslash if need, lower case, windows style.
        /// </summary>
        /// <param name="path">Raw path input.</param>
        /// <returns>Rectified path.</returns>
        private string RectifyPath(string path)
        {
            if(path != "0" && path.Length >= 1)
            {
                path += (path.EndsWith("/") || path.EndsWith("\\")) ? "" : "\\";
                path = path.Replace("/", "\\");
            }
            return path.ToLower();
        }


        /// <summary>
        /// Ask source dir, rectify, check exist loop.
        /// </summary>
        /// <returns>Source path or "0".</returns>
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


        /// <summary>
        /// Check destination valid: exist or "0", not same source, not source inside dest.
        /// </summary>
        /// <param name="src">Source path.</param>
        /// <param name="dst">Dest path.</param>
        /// <returns>True if ok.</returns>
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


        /// <summary>
        /// Ask dest dir with src, validate loop.
        /// </summary>
        /// <param name="src">Source to check against.</param>
        /// <returns>Dest path or "0".</returns>
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


        /// <summary>
        /// Format byte size to human readable like 1.2Go, To for TB.
        /// </summary>
        /// <param name="octet">Size in bytes.</param>
        /// <returns>Formatted string.</returns>
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


        /// <summary>
        /// Draw progress bar with # and . base on percent.
        /// </summary>
        /// <param name="percent">Progress 0-100.</param>
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


        /// <summary>
        /// Update top screen with current backup state: file, remaining, progress.
        /// Use cursor position to refresh.
        /// </summary>
        /// <param name="name">Backup name.</param>
        /// <param name="fileLeft">Files remaining count.</param>
        /// <param name="leftSize">Remaining size bytes.</param>
        /// <param name="curSize">Current file size.</param>
        /// <param name="percent">Progress percent.</param>
        public void DisplayCurrentState(string name, int fileLeft, long leftSize, long curSize, int percent)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Backup: {name}");
            Console.WriteLine($"{ResourceManager.GetString("CurrentFile")}: {DisplaySize(curSize)}");
            Console.WriteLine($"{ResourceManager.GetString("FilesRemaining")}: {fileLeft}");
            Console.WriteLine($"{ResourceManager.GetString("SizeRemaining")}: {DisplaySize(leftSize)}");
            DisplayProgressBar(percent);
        }


        /// <summary>
        /// Show backup finish recap with time and full progress bar.
        /// </summary>
        /// <param name="name">Backup name.</param>
        /// <param name="transfertTime">Time in ms.</param>
        public void DisplayBackupRecap(string name, double transfertTime)
        {
            Console.WriteLine("\n\n" +
                "Backup : " + name + " " + ResourceManager.GetString("Completed") + "\n"
                +"\n" + ResourceManager.GetString("Duration") + " : " + transfertTime + "ms\n"
            );
            DisplayProgressBar(100);
        }


        /// <summary>
        /// Print error for specific file.
        /// </summary>
        /// <param name="name">File name failed.</param>
        public void DisplayFiledError(string name)
        {
            Console.WriteLine(ResourceManager.GetString("FailedForFile") + " " + name);
        }


        /// <summary>
        /// Menu for choose save to remove, check not empty.
        /// </summary>
        /// <returns>Index 1-based or 0 none.</returns>
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


        /// <summary>
        /// Menu for launch backup, all or single.
        /// </summary>
        /// <returns>1 for all, or index+1 for single.</returns>
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
