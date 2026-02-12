using System;
using System.Collections.Generic;
using System.Linq;

namespace easySave_BMT.ViewModel_.CommandLine
{
    public class CommandLineParser
    {
        public void HandleCommandLine(string[] args, CommandLineBackupRunner runner)
        {
            string backupArg = args[1];
            
            if (args.Length > 2)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nWarning: Only the first argument after the executable name is used.");
                Console.WriteLine("Use semicolons (;) to separate multiple backup indices.");
                Console.ResetColor();
            }
            
            List<int> backupIndices = ParseCommandLineArguments(backupArg);

            if (backupIndices != null && backupIndices.Count > 0)
            {
                runner.ExecuteCommandLineBackups(backupIndices);
            }
            else
            {
                ShowUsageError();
            }
        }

        private List<int> ParseCommandLineArguments(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
                return null;

            List<int> indices = new List<int>();

            try
            {
                string[] parts = argument.Split(';');

                foreach (string part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                        continue;

                    if (part.Contains("-"))
                    {
                        string[] range = part.Split('-');
                        if (range.Length == 2)
                        {
                            int start = int.Parse(range[0].Trim());
                            int end = int.Parse(range[1].Trim());

                            if (start > 0 && end > 0 && start <= end)
                            {
                                for (int i = start; i <= end; i++)
                                {
                                    if (!indices.Contains(i))
                                        indices.Add(i);
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        int index = int.Parse(part.Trim());
                        if (index > 0 && !indices.Contains(index))
                            indices.Add(index);
                        else if (index <= 0)
                            return null;
                    }
                }

                indices.Sort();
                return indices;
            }
            catch
            {
                return null;
            }
        }

        private void ShowUsageError()
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
