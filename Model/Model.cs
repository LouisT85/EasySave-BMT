using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using easySave_BMT.ViewModel_;
using EasyLog;
using EasyLog.Models;


namespace easySave_BMT.Model_

{
    public class Model 
    {
        // --- Attributes ---

        private EasyLogger logger;
        private string logPath;
        private string backupsaveSavePath = "./BackupsaveSave.json";
        public List<Save> saves { get; set; }

        // Prepare options to indent JSON Files
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        }; 

        // --- Constructor ---
        public Model()
        {
            // Initalize save List
            this.saves = new List<Save>();

            logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EasySave",
                "Logs"
            );

            logger = new EasyLogger(logPath);
        }


        // --- Methods ---
        // Add save
        public int AddSave(string name, string src, string dst, BackupType backupType)
        {
            try
            {
                // Add save in the program (at the end of the List)
                this.saves.Add(new Save(name, src, dst, backupType));
                AddLogInJSONFile();

                // Return Success Code
                return 101;
            }
            catch
            {
                // Return Error Code
                return 201;
            }
        }

        // Remove save
        public int RemoveSave(int index)
        {
            try
            {
                // Remove save from the program (at index)
                this.saves.RemoveAt(index);
                AddLogInJSONFile();

                // Return Success Code
                return 103;
            }
            catch
            {
                // Return Error Code
                return 203;
            }
        }

        // Load saves and States at the beginning of the program
        public int CreateLogs()
        {
            // Check if backupsaveSave.json File exists
            if (File.Exists(backupsaveSavePath))
            {
                try
                {
                    // Read saves from JSON File (from ./BackupsaveSave.json) (use Save() constructor)
                    this.saves = JsonSerializer.Deserialize<List<Save>>(File.ReadAllText(this.backupsaveSavePath));
                }
                catch
                {
                    // Return Error Code
                    return 200;
                }
            }
            // Return Success Code
            return 100;
        }

        // Add log in JSON file
        public void AddLogInJSONFile()
        {
            // Write save list into JSON file (at ./BackupsaveSave.json)
            File.WriteAllText(this.backupsaveSavePath, JsonSerializer.Serialize(this.saves, this.jsonOptions));
        }

        public bool CopyFile(
            Save save,
            FileInfo currentFile,
            long curSize,
            string dst,
            long leftSize,
            int totalFile,
            int fileIndex,
            int pourcent)
        {
            DateTime startTimeFile = DateTime.Now;

            string curDirPath = currentFile.DirectoryName;
            string dstDirectory = dst;

            // management folder
            if (Path.GetRelativePath(save.src, curDirPath).Length > 1)
            {
                dstDirectory += Path.GetRelativePath(save.src, curDirPath) + "\\";

                if (!Directory.Exists(dstDirectory))
                {
                    Directory.CreateDirectory(dstDirectory);
                }
            }

            string dstFile = Path.Combine(dstDirectory, currentFile.Name);

            try
            {
                // update state
                save.state.UpdateState(
                    pourcent,
                    (totalFile - fileIndex),
                    leftSize,
                    currentFile.FullName,
                    dstFile
                );

                // copy of the file
                currentFile.CopyTo(dstFile, true);

                // log success (EasyLog.dll)
                logger.Write(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = save.name,
                    SourcePath = currentFile.FullName,
                    DestinationPath = dstFile,
                    FileSize = curSize,
                    TransferTimeMs = (long)(DateTime.Now - startTimeFile).TotalMilliseconds
                });

                return true;
            }
            catch
            {
                // error log (EasyLog.dll)
                logger.Write(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = save.name,
                    SourcePath = currentFile.FullName,
                    DestinationPath = dstFile,
                    FileSize = curSize,
                    TransferTimeMs = -1
                });

                return false;
            }
        }

    }
}
