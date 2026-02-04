using System;
using System.IO;
using System.Threading;
using easySave_BMT.ViewModel_;

namespace easySave_BMT
{
    class Program
    {
        static void Main(string[] args)
        {
            ViewModel viewModel = new ViewModel();
            
            if (args.Length > 0)
            {
                // Mode ligne de commande
                ProcessCommandLine(args, viewModel);
            }
            else
            {
                // Mode interactif
                viewModel.RunApp();
            }
        }

        static void ProcessCommandLine(string[] args, ViewModel viewModel)
        {
            try
            {
                // Charger les sauvegardes
                int loadResult = viewModel.model.CreateLogs();
                
                if (loadResult != 100)
                {
                    Console.WriteLine("Erreur lors du chargement des sauvegardes.");
                    return;
                }

                // Analyser les arguments
                string argument = args[0];
                
                if (argument.Contains("-"))
                {
                    // Format: 1-3
                    string[] range = argument.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        start--; // Convertir en index 0-based
                        end--;
                        
                        for (int i = start; i <= end && i < viewModel.model.saves.Count; i++)
                        {
                            ExecuteBackup(viewModel, i);
                        }
                    }
                }
                else if (argument.Contains(";"))
                {
                    // Format: 1;3
                    string[] indices = argument.Split(';');
                    foreach (string indexStr in indices)
                    {
                        if (int.TryParse(indexStr, out int index))
                        {
                            ExecuteBackup(viewModel, index - 1);
                        }
                    }
                }
                else if (int.TryParse(argument, out int singleIndex))
                {
                    // Format: 1
                    ExecuteBackup(viewModel, singleIndex - 1);
                }
                else if (argument.ToLower() == "all")
                {
                    // Exécuter toutes les sauvegardes
                    for (int i = 0; i < viewModel.model.saves.Count; i++)
                    {
                        ExecuteBackup(viewModel, i);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
            }
        }

        static void ExecuteBackup(ViewModel viewModel, int saveIndex)
        {
            if (saveIndex >= 0 && saveIndex < viewModel.model.saves.Count)
            {
                var save = viewModel.model.saves[saveIndex];
                Console.WriteLine($"Début de la sauvegarde: {save.name}");
                
                // Utiliser la méthode LaunchBackupType du ViewModel
                int result = viewModel.LaunchBackupType(save);
                
                if (result == 104 || result == 105)
                {
                    Console.WriteLine($"Sauvegarde {save.name} terminée avec succès.");
                }
                else
                {
                    Console.WriteLine($"Erreur lors de la sauvegarde {save.name}: Code {result}");
                }
                
                // Marquer la fin
                viewModel.model.FinishBackup(save);
                
                // Pause entre les sauvegardes
                Thread.Sleep(1000);
            }
            else
            {
                Console.WriteLine($"Index invalide: {saveIndex + 1}");
            }
        }
    }
}