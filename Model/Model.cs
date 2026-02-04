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
        public Model()
        {
            this.saves = new List<Save>();
            
            // Charger la configuration
            config = Config.Load();
            
            // Configurer les chemins
            RealTimeState.SetFilePath(config.StateFilePath);
            
            // Créer le répertoire de logs si nécessaire
            Directory.CreateDirectory(config.LogDirectory);
            
            // Initialiser le logger avec le chemin configuré
            logger = new EasyLogger(config.LogDirectory);
            
            Console.WriteLine($"Logs directory: {config.LogDirectory}");
            Console.WriteLine($"State file: {config.StateFilePath}");
        }

        // --- Methods ---
        
        public int AddSave(string name, string src, string dst, BackupType backupType)
        {
            try
            {
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

        public int RemoveSave(int index)
        {
            try
            {
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

        public int CreateLogs()
        {
            return ReloadSavesFromFile();
        }

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
                    
                    SaveStates();
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

        public void SaveStates()
        {
            try
            {
                List<RealTimeState> states = new List<RealTimeState>();
                
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

            // Gestion des dossiers
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
                // Mettre à jour l'état
                save.state.UpdateState(
                    pourcent,
                    (totalFile - fileIndex),
                    leftSize,
                    currentFile.FullName,
                    dstFile
                );
                
                // Mettre à jour l'état en temps réel
                UpdateSaveState(save);

                // Copier le fichier
                currentFile.CopyTo(dstFile, true);
                
                // Calcul du temps de transfert
                long transferTime = (long)(DateTime.Now - startTimeFile).TotalMilliseconds;

                // Log de succès
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
                
                // Log d'erreur
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
        
        public void FinishBackup(Save save)
        {
            if (save.state != null)
            {
                save.state.UpdateState(100, 0, 0, "", "");
                UpdateSaveState(save);
            }
            
            // Marquer comme terminé
            save.state = null;
            UpdateSaveState(save);
        }
        
        public Config GetConfig()
        {
            return config;
        }
        
        public void UpdateConfig(string logDir, string statePath, string language)
        {
            config.UpdateFromUserInput(logDir, statePath, language);
            
            // Reconfigurer les chemins
            RealTimeState.SetFilePath(config.StateFilePath);
            
            // Recréer le logger avec le nouveau chemin
            Directory.CreateDirectory(config.LogDirectory);
            logger = new EasyLogger(config.LogDirectory);
        }
    }
}