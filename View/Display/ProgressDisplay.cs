using System;
using easySave_BMT.Resources_;

namespace easySave_BMT.View_
{
    public class ProgressDisplay : IProgressObserver
    {
        public void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Backup: {backupName}");
            Console.WriteLine($"{ResourceManager.GetString("CurrentFile")}: {SizeFormatter.Format(currentFileSize)}");
            Console.WriteLine($"{ResourceManager.GetString("FilesRemaining")}: {filesLeft}");
            Console.WriteLine($"{ResourceManager.GetString("SizeRemaining")}: {SizeFormatter.Format(sizeLeft)}");
            DisplayProgressBar(percent);
        }

        public void OnBackupComplete(string backupName, double transferTime)
        {
            Console.WriteLine("\n\n" +
                "Backup : " + backupName + " " + ResourceManager.GetString("Completed") + "\n"
                + "\n" + ResourceManager.GetString("Duration") + " : " + transferTime + "ms\n"
            );
            DisplayProgressBar(100);
        }

        public void OnFileError(string fileName)
        {
            Console.WriteLine(ResourceManager.GetString("FailedForFile") + " " + fileName);
        }

        private void DisplayProgressBar(int percent)
        {
            Console.BackgroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(ResourceManager.GetString("Progress") + ": [ " + percent + " %]");
            Console.ResetColor();

            Console.Write(" [");
            for (int i = 0; i < 100; i += 5)
            {
                if (percent > i)
                {
                    Console.Write("#");
                }
                else
                {
                    Console.Write(".");
                }
            }
            Console.Write("]\n\n");
        }
    }
}
