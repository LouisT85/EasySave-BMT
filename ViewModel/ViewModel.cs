using System;                      // Library .NET
using System.IO;                   // files/directory management
using System.Collections.Generic;  // List<list>, dictionnary etc.
using easySave_BMT.Model_;         // Importation of "Model" directory class(es)
using easySave_BMT.View_;          // Importation of "View" directory class(es)

namespace easySave_BMT.ViewModel_  // Creation of ViewModel namespace
{
    class ViewModel
    {
        public Model model;
        public View view;

        public ViewModel()
        {
            this.model = new Model();
            this.view = new View();
        }
        public void RunApp()
        {
            bool currentlyRunning = true;
            while (currentlyRunning)
            {
                switch (this.view.Menu())
                {
                    case 1:
                        //faire une méthode qui affiche les saves
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
                        //faire une méthode qui quitte la page
                }
            }
        }
    }
}
