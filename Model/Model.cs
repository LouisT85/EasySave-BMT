using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using easySave_BMT.ViewModel_;
using EasyLog;
using EasyLog.Models;
using System.Threading;
using easySave_BMT.Resources_;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Core logic class of the application. It manages the list of backup jobs, 
    /// file operations, logging orchestration, and configuration persistence.
    /// </summary>
    public class Model
    {
        private EasyLogger logger;
        private Config config;
        private string backupsaveSavePath = "./BackupSave.json";

        /// <summary>List of all configured backup jobs.</summary>
        public List<Save> saves { get; private set; }

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        /// <summary>
        /// Initializes the Model, loads user configuration, sets up the logger, 
        /// and initializes the state file paths.
        /// </summary>
        public Model()
        {
            this.saves = new List<Save>();
            config = Config.Load();

            // Initialize global resources based on config
            ResourceManager.SetLanguage(config.Language);
            RealTimeState.SetFilePath(config.StateFilePath);
            Directory.CreateDirectory(config.LogDirectory);
            logger = new EasyLogger(config.LogDirectory);

            Console.WriteLine($"Logs directory: {config.LogDirectory}");
            Console.WriteLine($"State file: {config.StateFilePath}");
        }

        /// <summary>
        /// Adds a new backup job to the list and persists the changes.
        /// </summary>
        /// <returns>Status code: 101 for success, 201 for failure.</returns>
        public int AddSave(string name, string src, string dst, BackupType backupType)
        {
            try
            {
                this.saves.Add(new Save(name, src, dst, backupType));
                AddLogInJSONFile();

                // Create initial inactive state in the state file
                var inactiveState = State.CreateInactiveState(name);
                RealTimeState.SaveStates(new List<RealTimeState> { inactiveState });

                return 101;
            }
            catch
            {
                return 201;
            }
        }

        /// <summary>
        /// Removes a backup job from the list at the specified index.
        /// </summary>
        /// <returns>Status code: 103 for success, 203 for failure.</returns>
        public int RemoveSave(int index)
        {
            try
            {
                string removedName = this.saves[index].name;
                this.saves.RemoveAt(index);
                AddLogInJSONFile();

                // Clean up the real-time state file
                RealTimeState.RemoveState(removedName);

                return 103;
            }
            catch
            {
                return 203;
            }
        }

        /// <summary>
        /// Wrapper method to trigger save data loading.
        /// </summary>
        public int CreateLogs()
        {
            return ReloadSavesFromFile();
        }

        /// <summary>
        /// Loads the list of backup jobs from the BackupSave.json file.
        /// </summary>
        /// <returns>Status code: 100 for success, 200 for error.</returns>
        public int ReloadSavesFromFile()
        {
            if (File.Exists(backupsaveSavePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(this.backupsaveSavePath);
                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        this.saves = JsonSerializer.Deserialize<List<Save>>(jsonContent);
                    }
                    else
                    {
                        this.saves = new List<Save>();
                    }

                    return 100;
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON Error: {jsonEx.Message}");
                    return 200;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading saves: {ex.Message}");
                    return 200;
                }
            }
            else
            {
                this.saves = new List<Save>();
                return 100;
            }
        }

        /// <summary>
        /// Serializes the current list of backup jobs to the JSON save file.
        /// </summary>
        public void AddLogInJSONFile()
        {
            try
            {
                string json = JsonSerializer.Serialize(this.saves, this.jsonOptions);
                File.WriteAllText(this.backupsaveSavePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes the current execution states of all jobs with the real-time state file.
        /// </summary>
        public void SaveStates()
        {
            try
            {
                List<RealTimeState> statesToSave = new List<RealTimeState>();

                foreach (var save in this.saves)
                {
                    RealTimeState state;
                    if (save.state != null)
                    {
                        state = save.state.ToRealTimeState(save.name);
                    }
                    else
                    {
                        state = State.CreateInactiveState(save.name);
                    }
                    statesToSave.Add(state);
                }

                RealTimeState.SaveStates(statesToSave);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving states: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the real-time file for a single backup job.
        /// </summary>
        /// <param name="save">The backup job to update.</param>
        public void UpdateSaveState(Save save)
        {
            try
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

                RealTimeState.SaveStates(new List<RealTimeState> { state });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating state: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs the physical copy of a file, updates progress state, and logs the operation.
        /// </summary>
        /// <returns>True if the file was copied successfully, otherwise false.</returns>
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

            // Handle sub-directory structure at the destination
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
                // Update dynamic state before starting the copy
                save.state.UpdateState(
                    pourcent,
                    (totalFile - fileIndex),
                    leftSize,
                    currentFile.FullName,
                    dstFile
                );

                UpdateSaveState(save);

                // Perform file copy
                currentFile.CopyTo(dstFile, true);

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

                // Log error (TransferTime set to -1)
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

        /// <summary>
        /// Marks a backup job as finished, resets its progress state, and updates the persistence file.
        /// </summary>
        public void FinishBackup(Save save)
        {
            if (save.state != null)
            {
                save.state.UpdateState(100, 0, 0, "", "");
                UpdateSaveState(save);
            }

            save.state = null;
            UpdateSaveState(save);
        }

        /// <summary>
        /// Gets the current global configuration.
        /// </summary>
        public Config GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Updates application settings, including language, log directory, and state file path.
        /// </summary>
        public void UpdateConfig(string logDir, string statePath, string language)
        {
            config.UpdateFromUserInput(logDir, statePath, language);

            if (!string.IsNullOrWhiteSpace(language))
            {
                ResourceManager.SetLanguage(language);
            }

            RealTimeState.SetFilePath(config.StateFilePath);

            // Refresh logger with new directory
            Directory.CreateDirectory(config.LogDirectory);
            logger = new EasyLogger(config.LogDirectory);
        }
    }
}