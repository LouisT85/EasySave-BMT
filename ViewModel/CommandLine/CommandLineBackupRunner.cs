using System;
using System.Collections.Generic;
using easySave_BMT.Model_;
using easySave_BMT.ViewModel_.Backup;

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
            List<(int Index, Save Save)> validRuns = new List<(int Index, Save Save)>();

            foreach (int index in backupIndices)
            {
                int arrayIndex = index - 1;

                if (arrayIndex >= 0 && arrayIndex < _viewModel.model.saves.Count)
                {
                    Save save = _viewModel.model.saves[arrayIndex];
                    validRuns.Add((index, save));
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

            if (validRuns.Count > 0)
            {
                Console.WriteLine("Parallel execution started...");
                foreach (var run in validRuns.OrderBy(r => r.Index))
                {
                    Console.WriteLine($"Queued backup {run.Index}: {run.Save.name}");
                }

                var results = _viewModel.backupLauncher.LaunchBackupsInParallel(validRuns.Select(v => v.Save).ToList());
                var resultBySaveName = results.ToDictionary(r => r.Save.name, r => r.Result, StringComparer.OrdinalIgnoreCase);

                foreach (var run in validRuns.OrderBy(r => r.Index))
                {
                    int result = resultBySaveName.TryGetValue(run.Save.name, out int value) ? value : 216;

                    if (result == 104 || result == 105)
                    {
                        _viewModel.model.FinishBackup(run.Save);
                        successCount++;
                        executedBackups.Add($"{run.Index} - {run.Save.name}");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Backup {run.Index} completed successfully\n");
                        Console.ResetColor();
                    }
                    else if (result == 216)
                    {
                        _viewModel.model.FinishBackup(run.Save);
                        errorCount++;
                        failedBackups.Add($"{run.Index} - {run.Save.name} (partial)");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠ Backup {run.Index} completed with errors\n");
                        Console.ResetColor();
                    }
                    else
                    {
                        errorCount++;
                        failedBackups.Add($"{run.Index} - {run.Save.name}");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"✗ Backup {run.Index} failed (Error {result})\n");
                        Console.ResetColor();
                    }
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
