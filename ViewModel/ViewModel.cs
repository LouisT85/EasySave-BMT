using System;                      // Library .NET
using System.IO;                   // files/directory management
using System.Collections.Generic;  // List<list>, dictionnary etc.
using easySave_BMT.Model_;         // Importation of "Model" directory class(es)
using easySave_BMT.View_;
using System.Xml.Linq;          // Importation of "View" directory class(es)

namespace easySave_BMT.ViewModel_  // Creation of ViewModel namespace
{
    public class ViewModel
    {
        public Model model;
        public View view;

        public ViewModel()
        {
            this.model = new Model();
            this.view = new View(this);
        }
        public void RunApp()
        {
            // Load existing saves from JSON file
            int loadResult = model.CreateLogs();
            
            if (loadResult == 100)
            {
                Console.WriteLine("Application EasySave - BMT chargée avec succès !");
                view.DisplayMessage(100);
            }
            else
            {
                Console.WriteLine("Erreur lors du chargement des sauvegardes.");
                view.DisplayMessage(loadResult);
            }
            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                switch (this.view.Menu())
                {
                    case 1:
                        DisplaySaves();
                        break;
                    case 2:
                        AddSave();
                        break;
                    case 3:
                        RemoveSave();
                        break;
                    case 4:
                        //faire une méthode qui fait la backup du travail de sauvegarde
                        break;
                    case 5:
                        //faire une méthode qui permet d'entrer dans un menu de configuration ou de changer la langue
                        break;
                    case 6:
                        currentlyRunning = false; // quitte la page
                        Console.WriteLine("Merci d'avoir utilisé EasySave - BMT !");
                        Console.WriteLine("Appuyez sur une touche pour quitter...");
                        Console.ReadKey();
                        break;
                    default:
                        this.view.DisplayMessage(206); // Invalid choice
                        break;
                }
            }
        }
        private void DisplaySaves() // Method used in case 1, used to display all saves jobs
        {
            // CRITICAL FIX: Always reload from JSON before displaying
            int reloadResult = this.model.ReloadSavesFromFile();
            
            if (reloadResult == 100) // Success
            {
                if (this.model.saves.Count > 0)
                {
                    this.view.DisplayAllSaves();
                }
                else
                {
                    this.view.DisplayMessage(204); // Empty list
                }
            }
            else
            {
                this.view.DisplayMessage(reloadResult); // Display error code
            }
        }
        
        private void AddSave() // method used in case 2, used to add a new save job
        {
            if(this.model.saves.Count < 5)
            {
                string addSaveName = view.SaveName();
                if (addSaveName == "0") return;
                string addSaveSrc = view.SaveSrc();
                if (addSaveSrc == "0") return;
                string addSaveDest = view.SaveDst(addSaveSrc);
                if (addSaveDest == "0") return;
                BackupType AddSaveBackupType;
                switch(view.AddSaveBackupType())
                {
                    case 0:
                        return;
                    case 1:
                        AddSaveBackupType = BackupType.FULL;
                        break;
                    case 2:
                        AddSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                    default:
                        AddSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                }
                this.view.DisplayMessage(model.AddSave(addSaveName, addSaveSrc, addSaveDest, AddSaveBackupType));
            }
            else
            {
                this.view.DisplayMessage(205);
            }
        }
        
        //Remove a save
        private void RemoveSave()
        {
            if (this.model.saves.Count > 0)
            {
                int choice = view.RemovesaveChoice();
                if (choice == 0) return; // User chose to go back
                
                // Adjust for 1-based indexing in display
                int index = choice - 1;
                
                if (index >= 0 && index < this.model.saves.Count)
                {
                    this.view.DisplayMessage(model.RemoveSave(index));
                }
                else
                {
                    this.view.DisplayMessage(206); // Invalid choice
                }
            }
            else
            {
                this.view.DisplayMessage(204); // Empty list
            }
        }
        
        /*private void LaunchBackupsave()
        {
            if(this.model.saves.Count > 0)
            {
                int userChoice = view.LaunchBackupChoice();
                switch (userChoice)
                {
                    case 0:
                        return;
                    case 1:
                        foreach(Save save in this.model.saves)
                        {
                            this.view.DisplayMessage(LaunchBackupType(save));
                            this.view.DisplayMessage(4);
                        }
                        break;
                    default:

                }
            }
        }*/
    }
}
