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
            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                switch (this.view.Menu())
                {
                    case 1:
                        DisplaySaves();
                        break;
                    case 2:
                        //faire une méthode qui fait un nv travail de sauvegarde
                        break;
                    case 3:
                        //supprimer un travail de sauvegarde
                        break;
                    case 4:
                        //faire une méthode qui fait la backup du travail de sauvegarde
                        break;
                    case 5:
                        //faire une méthode qui permet d'entrer dans un menu de configuration ou de changer la langue
                        break;
                    case 6:
                        currentlyRunning = false; // quitte la page
                        break;
                    default:
                        //faire une méthode qui retourne un message (view)
                        break;
                }
            }
        }
        private void DisplaySaves() // Method used in case 1, used to display all saves jobs
        {
            if (this.model.saves.Count > 0)
            {
                this.view.DisplayAllSaves();
            }
            else
            {
                this.view.DisplayMessage(204);
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
                string addSaveDest = view.SaveDst(addsaveSrc);
                if (addSaveDest == "0") return;
                BackupType addSaveBackupType;
                switch (view.addSaveBackupType)
                {
                    case 0:
                        return;
                    case 1:
                        addSaveBackupType = BackupType.FULL;
                        break;
                    case 2:
                        addSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                    default:
                        addSaveBackupType = BackupType.DIFFERENTIAL;
                        break;
                }
                this.view.DisplayMessage(model.AddSave(addSaveName, addSaveSrc, addSaveDest, addSaveBackupType));
            }
            else
            {
                this.view.DisplayMessage(205);
            }
        }
        private void LaunchBackupsave()
        {
            if(this.model.saves.Count > 0)
            {
                int userChoice = view.LaunchBackupChoice();
                switch (userChoice)
                {
                    case 0:
                        return;
                    case 1:
                        foreach(save save in this.model.saves)
                        {
                            this.view.DisplayMessage(LaunchBackupType(save));
                            this.view.DisplayMessage(4);
                        }
                        break;
                    default:

                }
            }
        }
    }
}
