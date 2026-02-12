using System;
using System.IO;
using System.Collections.Generic;
using easySave_BMT.Resources_;
using easySave_BMT.Model_;

namespace easySave_BMT.View_
{
    public class BackupInputService
    {
        private readonly ValidationService validationService;
        private readonly MessageDisplay messageDisplay;

        public BackupInputService(ValidationService validationService, MessageDisplay messageDisplay)
        {
            this.validationService = validationService;
            this.messageDisplay = messageDisplay;
        }

        public string AskForBackupName(List<Save> existingSaves)
        {
            Console.Clear();
            Console.WriteLine(ResourceManager.GetString("BackupSettings"));
            messageDisplay.Display(2);

            Console.WriteLine("\n" + ResourceManager.GetString("EnterName"));
            string name = Console.ReadLine();

            while (!validationService.ValidateBackupName(name, existingSaves))
            {
                name = Console.ReadLine();
            }
            return name;
        }

        public string AskForSourcePath()
        {
            Console.WriteLine("\n" + ResourceManager.GetString("EnterSourceDirectory"));
            string src = PathFormatter.Rectify(Console.ReadLine());

            while (!Directory.Exists(src) && src != "0")
            {
                messageDisplay.Display(211);
                src = PathFormatter.Rectify(Console.ReadLine());
            }
            return src;
        }

        public string AskForDestinationPath(string sourcePath)
        {
            Console.WriteLine("\n" + ResourceManager.GetString("EnterDestinationDirectory"));
            string dst = PathFormatter.Rectify(Console.ReadLine());

            while (!validationService.ValidateDestinationPath(sourcePath, dst))
            {
                dst = PathFormatter.Rectify(Console.ReadLine());
            }
            return dst;
        }

        public void DisplayBackupsList(List<Save> saves)
        {
            Console.Clear();
            Console.WriteLine(ResourceManager.GetString("BackupList") + " : ");
            DisplaySavesJobReport(saves, 1);
            messageDisplay.Display(1);
        }

        public int AskForBackupToDelete(List<Save> saves)
        {
            if (saves.Count == 0)
            {
                messageDisplay.Display(204);
                return 0;
            }

            string[] items = new string[saves.Count];
            for (int i = 0; i < saves.Count; i++)
                items[i] = $"{i + 1} - {saves[i].name}";

            MenuDisplay menu = new MenuDisplay();
            return menu.ShowInteractiveMenu(ResourceManager.GetString("DeleteBackup"), items);
        }

        public int AskForBackupToLaunch(List<Save> saves)
        {
            string[] items = new string[saves.Count + 1];
            items[0] = "1 - " + ResourceManager.GetString("BackupAll");
            for (int i = 0; i < saves.Count; i++)
                items[i + 1] = $"{i + 2} - {saves[i].name}";

            MenuDisplay menu = new MenuDisplay();
            return menu.ShowInteractiveMenu(ResourceManager.GetString("LaunchBackup"), items);
        }

        private void DisplaySavesJobReport(List<Save> saves, int shift)
        {
            for (int i = 0; i < saves.Count; i++)
            {
                Console.WriteLine(
                    "\n" + (i + shift) + " - " + ResourceManager.GetString("Name") + ": " + saves[i].name
                    + "\n      " + ResourceManager.GetString("Source") + ": " + saves[i].src
                    + "\n      " + ResourceManager.GetString("Destination") + ": " + saves[i].dst
                    + "\n      " + ResourceManager.GetString("Type") + ": " + saves[i].backupType
                );
            }
        }
    }
}
