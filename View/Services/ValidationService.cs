using System;
using System.IO;
using System.Collections.Generic;
using easySave_BMT.Resources_;
using easySave_BMT.Model_;

namespace easySave_BMT.View_
{
    public class ValidationService
    {
        private readonly MessageDisplay messageDisplay;

        public ValidationService(MessageDisplay messageDisplay)
        {
            this.messageDisplay = messageDisplay;
        }

        public bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            try
            {
                Path.GetFullPath(path);

                char[] invalidChars = Path.GetInvalidPathChars();
                foreach (char c in invalidChars)
                {
                    if (path.Contains(c)) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateLogDirectory(string path)
        {
            if (path == "0") return true;

            if (!IsValidPath(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            try
            {
                Directory.CreateDirectory(path);

                string testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("NoWritePermission"));
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("Error") + ": " + ex.Message);
                Console.ResetColor();
                return false;
            }
        }

        public bool ValidateStateFilePath(string path)
        {
            if (path == "0") return true;

            if (!path.Contains(".") || !path.EndsWith(".json"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("StateFilePathWarning"));
                Console.ResetColor();
                return false;
            }

            if (!IsValidPath(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("InvalidPath"));
                Console.ResetColor();
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);

                string testFile = Path.Combine(directory, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("NoWritePermission"));
                Console.ResetColor();
                return false;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ResourceManager.GetString("Error") + ": " + ex.Message);
                Console.ResetColor();
                return false;
            }
        }

        public bool ValidateBackupName(string name, List<Save> existingSaves)
        {
            int length = name.Length;
            if (length >= 1 && length <= 20)
            {
                if (!existingSaves.Exists(save => save.name == name))
                {
                    return true;
                }
                messageDisplay.Display(214);
                return false;
            }
            messageDisplay.Display(215);
            return false;
        }

        public bool ValidateDestinationPath(string src, string dst)
        {
            if (dst == "0")
            {
                return true;
            }
            else if (Directory.Exists(dst))
            {
                if (src != dst)
                {
                    if (dst.Length > src.Length)
                    {
                        if (src != dst.Substring(0, src.Length))
                        {
                            return true;
                        }
                        else
                        {
                            messageDisplay.Display(217);
                            return false;
                        }
                    }
                    return true;
                }
                messageDisplay.Display(212);
                return false;
            }
            messageDisplay.Display(213);
            return false;
        }
    }
}
