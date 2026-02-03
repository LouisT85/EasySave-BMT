using System;
using System.IO;
using easySave_BMT.ModelView_;

namespace easySave_BMT.View_
{
    public class View
    {
        //Default ctor
        private ViewModel viewModel;

        public View(ViewModel viewModel)
        {
            this.viewModel = viewModel;
        }
            
        //Menu display    
        public int Menu()
        {
            Console.Clear();
            Console.WriteLine(
                "===== Easy Save - BMT =====" +
                "\n1 - Afficher les sauvegardes" +
                "\n2 - Ajouter une sauvegarde" +
                "\n3 - Faites un backup" +
                "\n4 - Supprimer une sauvegarde" +
                "\n5 - Quitter" 
                );

                
        }

        //Add SaveName

        private int CheckChoiceMenu(string inputUser, int minEntry, int maxEntry)
        {
            
        }

        //Display message on console
        public DisplayMessage(int id)
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
                        Console.WriteLine("\nLe fichier à été ajouté avec succès !!!");
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
    }
}