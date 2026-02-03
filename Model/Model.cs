using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using easySave_BMT.ViewModel_;

namespace easySave_BMT.Model_

{
    public class Model 
    {
        // --- Attributes ---
        private string backupsaveSavePath = "./BackupsaveSave.json";
        public List<save> saves { get; set; }

        // Prepare options to indent JSON Files
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        }; 

        // --- Constructor ---
        public Model()
        {
            // Initalize save List
            this.saves = new List<save>();
        }


        // --- Methods ---
        // Add save
        public int AddSave(string _name, string _src, string _dst, BackupType _backupType)
        {
            try
            {
                // Add save in the program (at the end of the List)
                this.saves.Add(new save(_name, _src, _dst, _backupType));
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
        public int RemoveSave(int _index)
        {
            try
            {
                // Remove save from the program (at index)
                this.saves.RemoveAt(_index);
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
                    // Read saves from JSON File (from ./BackupsaveSave.json) (use save() constructor)
                    this.saves = JsonSerializer.Deserialize<List<save>>(File.ReadAllText(this.backupsaveSavePath));
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

        public bool CopyFile(save _save, FileInfo _currentFile, long _curSize, string _dst, long _leftSize, int _totalFile, int fileIndex, int _pourcent)
        {
            // Time at when file copy start (use by SaveLog())
            DateTime startTimeFile = DateTime.Now;
            string curDirPath = _currentFile.DirectoryName;
            string dstDirectory = _dst;

            // If there is a directoy, we add the relative path from the directory dst
            if (Path.GetRelativePath(_save.src, curDirPath).Length > 1)
            {
                dstDirectory += Path.GetRelativePath(_save.src, curDirPath) + "\\";

                // If the directory dst doesn't exist, we create it
                if (!Directory.Exists(dstDirectory))
                {
                    Directory.CreateDirectory(dstDirectory);
                }
            }

            // Get the current dstFile
            string dstFile = dstDirectory + _currentFile.Name;

            /*try
            {
                // Update the current save status
                _save.state.UpdateState(_pourcent, (_totalFile - fileIndex), _leftSize, _currentFile.FullName, dstFile);
                AddLogInJSONFile();

                // Copy the current file
                _currentFile.CopyTo(dstFile, true);

                // Save Log
                _save.SaveLog(startTimeFile, _currentFile.FullName, dstFile, _curSize, false);
                return true;
            }
            catch
            {
                _save.SaveLog(startTimeFile, _currentFile.FullName, dstFile, _curSize, true);
                return false;
            }*/
        }
    }
}
