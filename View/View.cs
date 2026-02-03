using System;
using System.IO;
using easySave_BMT.ModelView_;

namespace easySave_BMT.View_
{
    public class View
    {
        //Default ctor
        private ViewModel viewModel;

        public View(ViewModel _viewModel)
        {
            this.viewModel = _viewModel;
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

        private int C
    }
}