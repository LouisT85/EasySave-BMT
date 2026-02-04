using System;
using System.IO;
using easySave_BMT.ViewModel_;

namespace easySave_BMT.View_
{
    public class View
    {
        
        private ViewModel viewModel;

        //ctor
        public View(ViewModel viewModel)
        {
            this.viewModel = viewModel;
        }
            
        //Menu display    
        public int Menu()
        {
            Console.Clear();
            Console.WriteLine("=== Easy Save - BMT ===");
            string[] menuItems = {
                "1 - Afficher les sauvegardes",
                "2 - Ajouter une sauvegarde", 
                "3 - Supprimer une sauvegarde",
                "4 - Faire une backup",
                "5 - Changer de langue"
            };
        
            Console.WriteLine("");
            Console.WriteLine("6 - Quitter");
        
            int selectedIndex = 0;
            bool selecting = true;
        
            while (selecting)
            {
                Console.Clear();
                Console.WriteLine("=== Easy Save - BMT ===");
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
                    Console.WriteLine("> 6 - Quitter");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("  6 - Quitter");
                }
        
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex == 0) ? 5 : selectedIndex - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex == 5) ? 0 : selectedIndex + 1;
                        break;
                    case ConsoleKey.Enter:
                        selecting = false;
                        break;
                    case ConsoleKey.Escape:
                        return 6;
                }
            }
        
            return (selectedIndex == 5) ? 6 : selectedIndex + 1;
        }
        
        //Display message on console
        public void DisplayMessage(int id)
        {
            if (id < 100)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                switch (id)
                {
                    //Information message
                    case 1:
                        Console.WriteLine("\nAppuyez sur Entrer pour afficher le menu . . . ." );
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
                        Console.WriteLine("\nAppuyer sur la touche Entrer pour en voir plus . . .");
                        Console.ReadLine();
                        break; 
                }
            }
            else if(id< 200)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                switch (id)
                {
                    //Success message from 100 to 199
                    case 100:
                        Console.WriteLine("\n########################### EASYSAVE BMT ########################");
                        DisplayMessage(1);
                        break;

                    case 101:
                        Console.WriteLine("\nLe fichier à été ajouté avec succès !!!");
                        DisplayMessage(1);
                        break;
                    
                    case 102:
                        Console.WriteLine("\nLe fichier à été sauvegardé avec succès !");
                        break;

                    case 103:
                        Console.WriteLine("\nLe fichier à été supprimé avec succès !!!");
                        DisplayMessage(1);
                        break;

                    case 104:
                        Console.WriteLine("\nBackup reussi !");
                        break;

                    case 105:
                        Console.WriteLine("\nAucune modification depuis la dernière sauvegarde complète !\n");
                        break;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                switch (id)
                {
                    // Error message from 200 to 299
                    case 200:
                        Console.WriteLine("\nRestaurer votre fichier de sauvegarde JSON. ");
                        DisplayMessage(1);
                        break;

                    case 201:
                        Console.WriteLine("\nÉchec de l'ajout .");
                        DisplayMessage(1);
                        break;

                    case 202:
                        Console.WriteLine("\nÉchec de sauvegarde .");
                        DisplayMessage(1);
                        break;

                    case 203:
                        Console.WriteLine("\nÉchec de la suppression .");
                        DisplayMessage(1);
                        break;

                    case 204:
                        Console.WriteLine("\nLa list est vide");
                        DisplayMessage(1);
                        break;

                    case 205:
                        Console.WriteLine("\nLa list est pleine");
                        DisplayMessage(1);
                        break;

                    case 206:
                        Console.WriteLine("\nEntrer une option valide");
                        break;

                    case 207:
                        Console.WriteLine("\nÉchec du transfert d'un fichier, le fichier source ou de destination n'existe pas.");
                        break;

                    case 208:
                        Console.WriteLine("\nLe type de sauvegarde sélectionné n'existe pas");
                        break;

                    case 209:
                        Console.WriteLine("\nÉchec de la copie du fichier.");
                        DisplayMessage(1);
                        break;

                    case 210:
                        Console.WriteLine("\nÉchec de la création du dossier de sauvegarde.");
                        DisplayMessage(1);
                        break;
                    case 211:
                        Console.WriteLine("\nDirectory 'existe pas. Veuillez entrer une source de directory valide. ");
                        break;

                    case 212:
                        Console.WriteLine("\nChoisissez un path différent de la source. ");
                        break;

                    case 213:
                        Console.WriteLine("\nDirectory n'existe pas. Veuillez entrer une direction de directory valide. ");
                        break;

                    case 214:
                        Console.WriteLine("\nLe nom est déjà pris. Veuillez entrer un autre nom.");
                        break;

                    case 215:
                        Console.WriteLine("\nEntrez un nom VALIDE(1 to 20 characters):");
                        break;

                    case 216:
                        Console.WriteLine("\nBackup terminé avec erreur.");
                        break;

                    case 217:
                        Console.WriteLine("\nLa destination directory ne peut pas être à l'intérieur de la source directory.");
                        break;

                    default:
                        Console.WriteLine("\nFailed : Erreur inconnue.");
                        DisplayMessage(1);
                        break;

                }
            }
            Console.ResetColor();
        }

        //Verify if input is integer
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

        //Add BackupType
        public int AddSaveBackupType()
        {
            Console.WriteLine(
                "\nChoisir le type de backup: "+
                "\n1.Complète "+
                "\n2.Differentielle"
            );
            return CheckChoiceMenu(Console.ReadLine(),0,2);
        }

        //Verify if the name is valid

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

        //Display all saves
        private void SavesJobReport(int shift)
        {
            var saves = this.viewModel.model.saves;

            for (int i =0; i<saves.Count; i++)
            {
                Console.WriteLine(
                    "\n" + (i + shift) + " - " + "Nom: " + saves[i].name
                    + "\n      Source: " + saves[i].src 
                    + "\n      Destination: " + saves[i].dst
                    +"\n       Type: " + saves[i].backupType
                );
            }
        }

        public void DisplayAllSaves()
        {
            Console.Clear();
            Console.WriteLine("liste des sauvegardes : ");

            //Display all saves
            SavesJobReport(1);
            DisplayMessage(1);
        }

        //Add a save name
        public string SaveName()
        {
            Console.Clear();
            Console.WriteLine("Paramètre de sauvegarde");
            DisplayMessage(2);

            Console.WriteLine("\nEntrer un nom (1 à 20 caractères):");
            string name = Console.ReadLine();

            //Verify name
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

        //source
        public string SaveSrc()
        {
            Console.WriteLine("\nEntrez la source du repertoire ");
            string src = RectifyPath(Console.ReadLine());

            //Verify the src is valid
            while(
                !Directory.Exists(src) && src != "000" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "" +
                "")
            {
                DisplayMessage(211);
                src = RectifyPath(Console.ReadLine());
            }
            return src;
        }

        

        //save destination

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
            Console.WriteLine("\nEntrer la destination du répertoire.");
            string dst = RectifyPath(Console.ReadLine());

            //Verfify if the destination is valid

            while (!ChecksaveDst(src, dst))
            {
                dst= RectifyPath(Console.ReadLine());
            }
            return dst;
        }

        //Display size
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
            Console.Write("Progression: [ " + percent + " %]");
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
            Console.Clear();
            Console.WriteLine(
                "Backup actuelle : " + name
                +"\nTaille des fichiers: " + DisplaySize(curSize)
                +"\nNombre de fchiers restants: "+fileLeft
                +"\nTaille des fichiers restants: "+DisplaySize(leftSize)+"\n"
            );
            DisplayProgressBar(percent);
        }

        public void DisplayBackupRecap(string name, double transfertTime)
        {
            Console.WriteLine("\n\n" + 
                "Backup : " + name + "terminé\n"
                +"\nDurée : " + transfertTime + "ms\n"
            );
            DisplayProgressBar(100);
        }

        public void DisplayFiledError(string name)
        {
            Console.WriteLine("Échec pour le fichier " + name);
        }
        
        //Verify if input is in the good range
        private int CheckChoiceMenu(string inputUser, int minEntry, int maxEntry)
        {
            while(!(CheckInt(inputUser) && (Int32.Parse(inputUser) >= minEntry && Int32.Parse(inputUser)<= maxEntry)))
            {
                DisplayMessage(206);
                inputUser = Console.ReadLine();
            }
            return Int32.Parse(inputUser);
        }

        //choose save to delete
        public int RemovesaveChoice()
        {
            Console.Clear();
            Console.WriteLine("Supprime une sauvegarde: ");

            //Display all saves
            SavesJobReport(1);
            DisplayMessage(2);

            //Display if userinput is a valid integer
            return CheckChoiceMenu(Console.ReadLine(), 0, this.viewModel.model.saves.Count);
        }

        //Choose the work to save
        public int LaunchBackupChoice()
        {
            Console.Clear();
            Console.WriteLine(
                "Choisissez le travail à sauvegarder : " +
                "\n\n1 - tout"
            );

            //Choose the work to save
            SavesJobReport(2);
            DisplayMessage(2);

            //Display if input is a valid integer
            return CheckChoiceMenu(Console.ReadLine(), 0, this.viewModel.model.saves.Count + 1);
        }
    }
}