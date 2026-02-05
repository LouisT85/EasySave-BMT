using System;
using System.IO;
using easySave_BMT.ViewModel_;
using easySave_BMT.Model_;

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
                        Console.WriteLine("> 0 - Retour");
                        Console.ResetColor();
                    }
                    else
                        Console.WriteLine("  0 - Retour");
                }

                Console.WriteLine("\n↑↓ pour naviguer | Entrée pour valider | Échap pour annuler");
                var key = Console.ReadKey(true);

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
                "1 - Afficher les sauvegardes",
                "2 - Ajouter une sauvegarde", 
                "3 - Supprimer une sauvegarde",
                "4 - Faire une backup",
                "5 - Configuration"
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
                    Console.WriteLine("> 6 - Quitter");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("  6 - Quitter");
                }
                
                Console.WriteLine("");
                Console.Write("↑↓ pour naviguer | 1-6 ou ↵ pour confirmer votre choix: ");

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
                "1 - Afficher la configuration actuelle",
                "2 - Modifier le répertoire des logs",
                "3 - Modifier le fichier d'état",
                "4 - Changer la langue (fr/en)"
            };

            return InteractiveMenu("Configuration", configItems);
        }

        public void DisplayCurrentConfiguration(Config config)
        {
            Console.Clear();
            Console.WriteLine("=== Configuration actuelle ===");
            Console.WriteLine("");
            Console.WriteLine($"Répertoire des logs: {config.LogDirectory}");
            Console.WriteLine($"Fichier d'état: {config.StateFilePath}");
            Console.WriteLine($"Langue: {config.Language}");
            Console.WriteLine("");
            Console.WriteLine("Appuyez sur Entrée pour continuer...");
            Console.ReadLine();
        }

        public string AskForLogDirectory()
        {
            Console.Clear();
            Console.WriteLine("=== Modification du répertoire des logs ===");
            Console.WriteLine("");
            Console.WriteLine("Laissez vide pour garder la valeur actuelle.");
            Console.Write("Nouveau répertoire des logs: ");
            
            string input = RectifyPath(Console.ReadLine());
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    Directory.CreateDirectory(input);
                    return input;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                    Console.WriteLine("Appuyez sur Entrée pour continuer...");
                    Console.ReadLine();
                }
            }
            return null;
        }

        public string AskForStateFilePath()
        {
            Console.Clear();
            Console.WriteLine("=== Configuration du chemin du fichier d'état ===");
            Console.WriteLine("");
            Console.WriteLine("Fichier d'état actuel : " + viewModel.model.GetConfig().StateFilePath);
            Console.WriteLine("");
            Console.WriteLine("IMPORTANT : Fournissez un chemin d’accès complet incluant le nom du fichier (par ex. C:\\EasySave\\state.json)");
            Console.WriteLine("Laissez vide pour garder le chemin actuel.");
            Console.WriteLine("");
            Console.Write("Nouveau chemin du fichier d'état : ");
            
            string input = Console.ReadLine();
            
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (!input.Contains("."))
                {
                    Console.WriteLine("Avertissement : Ceci correspond à un chemin vers un dossier, pas vers un fichier.");
                    Console.WriteLine("S'il vous plaît veuillez fournir un chemin incluant le nom du fichier (e.g., C:\\EasySave\\state.json)");
                    Console.WriteLine("Appuyez sur ↵ pour continuer...");
                    Console.ReadLine();
                    return null;
                }
                
                try
                {
                    string directory = Path.GetDirectoryName(input);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                        string testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                    return input;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur: {ex.Message}");
                    Console.WriteLine("Cet emplacement n'a probablement pas les permissions d'écriture.");
                    Console.WriteLine("Appuyez sur ↵ pour Continuer...");
                    Console.ReadLine();
                }
            }
            return null;
        }

        public string AskForLanguage()
        {
            string[] langItems = {
                "1 - Français (fr)",
                "2 - Anglais (en)"
            };

            int choice = InteractiveMenu("Changement de langue", langItems);

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
                Console.WriteLine("\nConfiguration mise à jour avec succès !");
                Console.WriteLine("Appuyez sur Entrée pour continuer...");
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
                        Console.WriteLine("\nAppuyez sur Entrer pour afficher le menu . . . .");
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
                "1 - Sauvegarde complète",
                "2 - Sauvegarde différentielle"
            };

            return InteractiveMenu("Type de sauvegarde", backupTypes);
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
            SavesJobReport(1);
            DisplayMessage(1);
        }

        public string SaveName()
        {
            Console.Clear();
            Console.WriteLine("Paramètre de sauvegarde");
            DisplayMessage(2);

            Console.WriteLine("\nEntrer un nom (1 à 20 caractères):");
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
            Console.WriteLine("\nEntrez la source du repertoire ");
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
            Console.WriteLine("\nEntrer la destination du répertoire.");
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
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Backup: {name}");
            Console.WriteLine($"Fichier actuel: {DisplaySize(curSize)}");
            Console.WriteLine($"Fichiers restants: {fileLeft}");
            Console.WriteLine($"Taille restante: {DisplaySize(leftSize)}");
            DisplayProgressBar(percent);
        }

        public void DisplayBackupRecap(string name, double transfertTime)
        {
            Console.WriteLine("\n\n" + 
                "Backup : " + name + " terminé\n"
                +"\nDurée : " + transfertTime + "ms\n"
            );
            DisplayProgressBar(100);
        }

        public void DisplayFiledError(string name)
        {
            Console.WriteLine("Échec pour le fichier " + name);
        }
        
        private int CheckChoiceMenu(string inputUser, int minEntry, int maxEntry)
        {
            while(!(CheckInt(inputUser) && (Int32.Parse(inputUser) >= minEntry && Int32.Parse(inputUser)<= maxEntry)))
            {
                DisplayMessage(206);
                inputUser = Console.ReadLine();
            }
            return Int32.Parse(inputUser);
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

            return InteractiveMenu("Supprimer une sauvegarde", items);
        }

        public int LaunchBackupChoice()
        {
            var saves = viewModel.model.saves;
            string[] items = new string[saves.Count + 1];
            items[0] = "1 - Tout sauvegarder";
            for (int i = 0; i < saves.Count; i++)
                items[i + 1] = $"{i + 2} - {saves[i].name}";

            return InteractiveMenu("Lancer une sauvegarde", items);
        }
    }
}
