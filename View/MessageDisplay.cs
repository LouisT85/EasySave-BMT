using System;
using easySave_BMT.Resources_;

namespace easySave_BMT.View_
{
    public class MessageDisplay
    {
        public void Display(int id)
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
                        Display(1);
                        break;

                    case 101:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileAddedSuccess"));
                        Display(1);
                        break;

                    case 102:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileSavedSuccess"));
                        break;

                    case 103:
                        Console.WriteLine("\n" + ResourceManager.GetString("FileDeletedSuccess"));
                        Display(1);
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
                        Display(1);
                        break;

                    case 201:
                        Console.WriteLine("\n" + ResourceManager.GetString("AddFailed"));
                        Display(1);
                        break;

                    case 202:
                        Console.WriteLine("\n" + ResourceManager.GetString("SaveFailed"));
                        Display(1);
                        break;

                    case 203:
                        Console.WriteLine("\n" + ResourceManager.GetString("DeleteFailed"));
                        Display(1);
                        break;

                    case 204:
                        Console.WriteLine("\n" + ResourceManager.GetString("ListEmpty"));
                        Display(1);
                        break;

                    case 205:
                        Console.WriteLine("\n" + ResourceManager.GetString("ListFull"));
                        Display(1);
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
                        Display(1);
                        break;

                    case 210:
                        Console.WriteLine("\n" + ResourceManager.GetString("CreateFolderFailed"));
                        Display(1);
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
                        Display(1);
                        break;
                }
            }
            Console.ResetColor();
        }
    }
}
