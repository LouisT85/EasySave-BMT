using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using easySave_BMT.Model_;

namespace easySave_BMT.ViewModel_.CommandLine
{
    public class CommandLineBackupRunner
    {
        private readonly ViewModel _viewModel;

        public CommandLineBackupRunner(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void ExecuteCommandLineBackups(List<int> backupIndices)
        {
            Console.WriteLine("\n=== Automatic Backup Execution ===\n");

            if (_viewModel.model.saves.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No backup configurations found.");
                Console.ResetColor();
                return;
            }

            int successCount = 0;
            int errorCount = 0;
            List<string> executedBackups = new List<string>();
            List<string> failedBackups = new List<string>();

            foreach (int index in backupIndices)
            {
                int arrayIndex = index - 1;

                if (arrayIndex >= 0 && arrayIndex < _viewModel.model.saves.Count)
                {
                    Save save = _viewModel.model.saves[arrayIndex];
                    Console.WriteLine($"Executing backup {index}: {save.name}");

                    int result = _viewModel._backupLauncher.LaunchBackupType(save);

                    if (result == 104 || result == 105)
                    {
                        _viewModel.model.FinishBackup(save);
                        successCount++;
                        executedBackups.Add($"{index} - {save.name}");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Backup {index} completed successfully\n");
                        Console.ResetColor();
                    }
                    else if (result == 216)
                    {
                        _viewModel.model.FinishBackup(save);
                        errorCount++;
                        failedBackups.Add($"{index} - {save.name} (partial)");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Backup {index} completed with errors\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        errorCount++;
                        failedBackups.Add($"{index} - {save.name}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ Backup {index} failed (Error {result})\n");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Backup index {index} does not exist (Available: 1-{_viewModel.model.saves.Count})\n");
                    Console.ResetColor();
                    errorCount++;
                    failedBackups.Add($"{index} - Not found");
                }
            }

            DisplaySummary(successCount, errorCount, executedBackups, failedBackups);
        }

        private void DisplaySummary(int successCount, int errorCount, List<string> executedBackups, List<string> failedBackups)
        {
            Console.WriteLine("\n=== Execution Summary ===");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successful: {successCount}");
            Console.ResetColor();

            if (errorCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed/Errors: {errorCount}");
                Console.ResetColor();
            }

            if (executedBackups.Count > 0)
            {
                Console.WriteLine("\nCompleted backups:");
                foreach (string backup in executedBackups)
                {
                    Console.WriteLine($"  ✓ {backup}");
                }
            }

            if (failedBackups.Count > 0)
            {
                Console.WriteLine("\nFailed backups:");
                foreach (string backup in failedBackups)
                {
                    Console.WriteLine($"  ✗ {backup}");
                }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
