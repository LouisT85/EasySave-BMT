using System;
using System.Collections.Generic;
using System.Linq;
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
            List<string> executedBackups = new();
            List<string> failedBackups = new();
            List<(int Index, Save Save)> selected = new();

            foreach (int index in backupIndices)
            {
                int arrayIndex = index - 1;

                if (arrayIndex >= 0 && arrayIndex < _viewModel.model.saves.Count)
                {
                    selected.Add((index, _viewModel.model.saves[arrayIndex]));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[x] Backup index {index} does not exist (Available: 1-{_viewModel.model.saves.Count})\n");
                    Console.ResetColor();
                    errorCount++;
                    failedBackups.Add($"{index} - Not found");
                }
            }

            bool usePriorityPolicy =
                _viewModel.backupLauncher.HasPriorityExtensionsConfigured() &&
                selected.Count > 1;

            if (usePriorityPolicy)
            {
                var blocked = new HashSet<Save>();
                var workload = selected.ToDictionary(
                    x => x.Save,
                    x => _viewModel.backupLauncher.GetFilePriorityCounts(x.Save));

                foreach (var item in selected)
                {
                    if (workload[item.Save].PriorityFiles <= 0) continue;

                    Console.WriteLine($"Executing priority files for backup {item.Index}: {item.Save.name}");
                    int phaseResult = _viewModel.backupLauncher.LaunchBackupType(
                        item.Save,
                        BackupLauncher.FileSelectionMode.PriorityOnly);

                    if (phaseResult == 104 || phaseResult == 105 || phaseResult == 216) continue;

                    blocked.Add(item.Save);
                    errorCount++;
                    failedBackups.Add($"{item.Index} - {item.Save.name}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[x] Backup {item.Index} failed during priority pass (Error {phaseResult})\n");
                    Console.ResetColor();
                }

                foreach (var item in selected)
                {
                    if (blocked.Contains(item.Save)) continue;

                    Console.WriteLine($"Executing remaining files for backup {item.Index}: {item.Save.name}");
                    int result = _viewModel.backupLauncher.LaunchBackupType(
                        item.Save,
                        BackupLauncher.FileSelectionMode.NonPriorityOnly,
                        allowResumeFromCompletedState: workload[item.Save].PriorityFiles > 0);

                    RegisterResult(
                        item.Index,
                        item.Save,
                        result,
                        ref successCount,
                        ref errorCount,
                        executedBackups,
                        failedBackups);
                }
            }
            else
            {
                foreach (var item in selected)
                {
                    Console.WriteLine($"Executing backup {item.Index}: {item.Save.name}");
                    int result = _viewModel.backupLauncher.LaunchBackupType(item.Save);

                    RegisterResult(
                        item.Index,
                        item.Save,
                        result,
                        ref successCount,
                        ref errorCount,
                        executedBackups,
                        failedBackups);
                }
            }

            DisplaySummary(successCount, errorCount, executedBackups, failedBackups);
        }

        private void RegisterResult(
            int index,
            Save save,
            int result,
            ref int successCount,
            ref int errorCount,
            List<string> executedBackups,
            List<string> failedBackups)
        {
            if (result == 104 || result == 105)
            {
                _viewModel.model.FinishBackup(save);
                successCount++;
                executedBackups.Add($"{index} - {save.name}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[ok] Backup {index} completed successfully\n");
                Console.ResetColor();
                return;
            }

            if (result == 216)
            {
                _viewModel.model.FinishBackup(save);
                errorCount++;
                failedBackups.Add($"{index} - {save.name} (partial)");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[!] Backup {index} completed with errors\n");
                Console.ResetColor();
                return;
            }

            errorCount++;
            failedBackups.Add($"{index} - {save.name}");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[x] Backup {index} failed (Error {result})\n");
            Console.ResetColor();
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
                    Console.WriteLine($"  [ok] {backup}");
                }
            }

            if (failedBackups.Count > 0)
            {
                Console.WriteLine("\nFailed backups:");
                foreach (string backup in failedBackups)
                {
                    Console.WriteLine($"  [x] {backup}");
                }
            }

            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();
        }
    }
}
