using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using easySave_BMT.ViewModel_;
using EasyLog;
using EasyLog.Models;
using System.Threading;

namespace easySave_BMT.Model_
{

    /// Main model class handling business logic and data management

    public class Model 
    {
        // --- Attributes ---
        private EasyLogger logger;
        private Config config;
        private string backupsaveSavePath = "./BackupSave.json";
        public List<Save> saves { get; private set; }

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        }; 

        // --- Constructor ---

        /// Initializes a new instance of the Model class

        public Model()
        {
            // Initialize save list
            this.saves = new List<Save>();
            
            // Load configuration
            config = Config.Load();
            
            // Configure paths
            RealTimeState.SetFilePath(config.StateFilePath);
            
            // Create log directory if needed
            Directory.CreateDirectory(config.LogDirectory);
            
            // Initialize logger
            logger = new EasyLogger(config.LogDirectory);
            
            Console.WriteLine($"Logs directory: {config.LogDirectory}");
            Console.WriteLine($"State file: {config.StateFilePath}");
        }

        // --- Methods ---
        

        /// Adds a new save job to the list

        public int AddSave(string name, string src, string dst, BackupType backupType)
        {
            try
            {
                // Add save in the program (at the end of the List)
                this.saves.Add(new Save(name, src, dst, backupType));
                AddLogInJSONFile();
                SaveStates();
                
                return 101;
            }
            catch
            {
                return 201;
            }
        }


        /// Removes a save job from the list

        public int RemoveSave(int index)
        {
            try
            {
                // Remove save from the program (at index)
                this.saves.RemoveAt(index);
                AddLogInJSONFile();
                SaveStates();
                
                return 103;
            }
            catch
            {
                return 203;
            }
        }

        /// Loads saves and states at the beginning of the program

        public int CreateLogs()
        {
            return ReloadSavesFromFile();
        }


        /// Reloads saves from the JSON file

        public int ReloadSavesFromFile()
        {
            // Check if backupsaveSave.json File exists
            if (File.Exists(backupsaveSavePath))
            {
                try
                {
                    // Read saves from JSON File (from ./BackupsaveSave.json)
                    string jsonContent = File.ReadAllText(this.backupsaveSavePath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        this.saves = JsonSerializer.Deserialize<List<Save>>(jsonContent);
                    }
                    else
                    {
                        this.saves = new List<Save>();
                    }
                    
                    // Return Success Code
                    return 100;
                }
                catch (JsonException jsonEx)
                {
                    // JSON parsing error
                    Console.WriteLine($"JSON Error: {jsonEx.Message}");
                    return 200;
                }
                catch (Exception ex)
                {
                    // General error
                    Console.WriteLine($"Error loading saves: {ex.Message}");
                    return 200;
                }
            }
            else
            {
                // File doesn't exist, initialize empty list
                this.saves = new List<Save>();
                return 100;
            }
        }


        /// Saves the current save list to JSON file

        public void AddLogInJSONFile()
        {
            try
            {
                // Write save list into JSON file (at ./BackupsaveSave.json)
                string json = JsonSerializer.Serialize(this.saves, this.jsonOptions);
                File.WriteAllText(this.backupsaveSavePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to JSON: {ex.Message}");
            }
        }

        /// Saves all states to the real-time state file

        public void SaveStates()
        {
            try
            {
                List<RealTimeState> states = new List<RealTimeState>();
                
                // Only save states for existing saves
                foreach (var save in this.saves)
                {
                    RealTimeState state;
                    if (save.state != null)
                    {
                        state = save.state.ToRealTimeState(save.name);
                    }
                    else
                    {
                        state = State.CreateEndState(save.name);
                    }
                    states.Add(state);
                }
                
                RealTimeState.SaveStates(states);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving states: {ex.Message}");
            }
        }


        /// Updates the real-time state for a specific save

        public void UpdateSaveState(Save save)
        {
            try
            {
                var existingStates = RealTimeState.LoadStates();
                var existingState = existingStates.Find(s => s.Name == save.name);
                
                if (existingState == null)
                {
                    existingState = new RealTimeState { Name = save.name };
                    existingStates.Add(existingState);
                }
                
                if (save.state != null)
                {
                    existingState.State = "ACTIVE";
                    existingState.SourceFilePath = save.state.currentPathSrc;
                    existingState.TargetFilePath = save.state.currentPathDest;
                    existingState.TotalFilesToCopy = save.state.totalFile;
                    existingState.TotalFilesSize = save.state.totalSize;
                    existingState.NbFilesLeftToDo = save.state.nbFileLeft;
                    existingState.Progression = save.state.progress;
                }
                else
                {
                    existingState.State = "END";
                    existingState.SourceFilePath = "";
                    existingState.TargetFilePath = "";
                    existingState.NbFilesLeftToDo = 0;
                    existingState.Progression = 0;
                }
                
                RealTimeState.SaveStates(existingStates);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating state: {ex.Message}");
            }
        }


        /// Copies a file and logs the operation
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

            // Management of subdirectories
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
                // Update state
                save.state.UpdateState(
                    pourcent,
                    (totalFile - fileIndex),
                    leftSize,
                    currentFile.FullName,
                    dstFile
                );
                
                // Update real-time state
                UpdateSaveState(save);

                // Copy the file
                currentFile.CopyTo(dstFile, true);
                
                // Calculate transfer time
                long transferTime = (long)(DateTime.Now - startTimeFile).TotalMilliseconds;

                // Log success
                logger.Write(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    BackupName = save.name,
                    SourcePath = currentFile.FullName,
                    DestinationPath = dstFile,
                    FileSize = curSize,
                    TransferTimeMs = transferTime
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying file: {ex.Message}");
                
                // Log error
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
        

        /// Marks a backup as finished in the state file

        public void FinishBackup(Save save)
        {
            if (save.state != null)
            {
                save.state.UpdateState(100, 0, 0, "", "");
                UpdateSaveState(save);
            }
            
            // Mark as ended
            save.state = null;
            UpdateSaveState(save);
        }
        

        /// Gets the current configuration

        public Config GetConfig()
        {
            return config;
        }
        

        /// Updates the application configuration

        public void UpdateConfig(string logDir, string statePath, string language)
        {
            config.UpdateFromUserInput(logDir, statePath, language);
            
            // Reconfigure paths
            RealTimeState.SetFilePath(config.StateFilePath);
            
            // Recreate logger with new path
            Directory.CreateDirectory(config.LogDirectory);
            logger = new EasyLogger(config.LogDirectory);
        }
    }
}