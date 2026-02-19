using System;
using System.Collections.Generic;

namespace easySave_BMT.ViewModel_.CommandLine
{
    /// <summary>
    /// Parses and validates backup indices provided by command-line arguments.
    /// </summary>
    public class CommandLineParser
    {
        /// <summary>
        /// Parses command-line arguments and launches selected backups.
        /// </summary>
        /// <param name="args">The full process argument list.</param>
        /// <param name="runner">The command-line runner service.</param>
        public void HandleCommandLine(string[] args, CommandLineBackupRunner runner)
        {
            if (args.Length < 2)
            {
                ShowUsageError();
                return;
            }

            string backupArg = args[1];

            if (args.Length > 2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nWarning: only the first argument after the executable name is used.");
                Console.WriteLine("Use semicolons (;) to separate multiple backup indices.");
                Console.ResetColor();
            }

            List<int> backupIndices = ParseCommandLineArguments(backupArg);
            if (backupIndices.Count > 0)
            {
                runner.ExecuteCommandLineBackups(backupIndices);
                return;
            }

            ShowUsageError();
        }

        private static List<int> ParseCommandLineArguments(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                return new List<int>();
            }

            var indices = new HashSet<int>();
            string[] parts = argument.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string part in parts)
            {
                if (part.Contains('-'))
                {
                    string[] range = part.Split('-', StringSplitOptions.TrimEntries);
                    if (range.Length != 2 || !int.TryParse(range[0], out int start) || !int.TryParse(range[1], out int end))
                    {
                        return new List<int>();
                    }

                    if (start <= 0 || end <= 0 || start > end)
                    {
                        return new List<int>();
                    }

                    for (int i = start; i <= end; i++)
                    {
                        indices.Add(i);
                    }

                    continue;
                }

                if (!int.TryParse(part, out int index) || index <= 0)
                {
                    return new List<int>();
                }

                indices.Add(index);
            }

            var result = new List<int>(indices);
            result.Sort();
            return result;
        }

        private static void ShowUsageError()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nInvalid command line argument format.");
            Console.WriteLine("Usage examples:");
            Console.WriteLine("  EasySave.exe 1;3;5       (execute backups 1, 3 and 5)");
            Console.WriteLine("  EasySave.exe 1-3;5       (execute backups 1, 2, 3 and 5)");
            Console.WriteLine("  EasySave.exe 1;2-4;7     (execute backups 1, 2, 3, 4 and 7)");
            Console.WriteLine("  EasySave.exe 1-5         (execute backups 1 to 5)");
            Console.ResetColor();
            Console.WriteLine("\nPress Enter to continue to interactive menu...");
            Console.ReadLine();
        }
    }
}
