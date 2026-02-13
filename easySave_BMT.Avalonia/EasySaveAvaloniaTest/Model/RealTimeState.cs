using System;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents the persistent model for real-time backup states, used for JSON serialization.
    /// It includes static methods to manage the state file on disk.
    /// </summary>
    public class RealTimeState
    {
        /// <summary>The name of the backup job.</summary>
        public string Name { get; set; }

        /// <summary>The date and time of the last update.</summary>
        public string Timestamp { get; set; }

        /// <summary>The path of the source file currently being processed.</summary>
        public string SourceFilePath { get; set; }

        /// <summary>The path of the target file currently being processed.</summary>
        public string TargetFilePath { get; set; }

        /// <summary>The current status of the job (e.g., ACTIVE, INACTIVE, END).</summary>
        public string State { get; set; }

        /// <summary>Total number of files to be copied for this job.</summary>
        public int TotalFilesToCopy { get; set; }

        /// <summary>Total size of files to be copied in bytes.</summary>
        public long TotalFilesSize { get; set; }

        /// <summary>Number of files remaining to be processed.</summary>
        public int NbFilesLeftToDo { get; set; }

        /// <summary>The current progress percentage (0-100).</summary>
        public int Progression { get; set; }

        /// <summary>Internal static storage for the state file's full path.</summary>
        private static string _stateFilePath;

        /// <summary>
        /// Configures the file path where states will be saved. 
        /// Automatically handles directory creation and ensures a valid filename.
        /// </summary>
        /// <param name="filePath">The directory or full file path for state persistence.</param>
        public static void SetFilePath(string filePath)
        {
            _stateFilePath = filePath;

            if (!string.IsNullOrEmpty(_stateFilePath))
            {
                // If the path is a directory or lacks an extension, append default filename
                if (Directory.Exists(_stateFilePath) || !_stateFilePath.Contains("."))
                {
                    _stateFilePath = Path.Combine(_stateFilePath, "state.json");
                }

                string directory = Path.GetDirectoryName(_stateFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not create directory '{directory}': {ex.Message}");
                        _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
                    }
                }
            }
            else
            {
                _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
            }
        }

        /// <summary>
        /// Saves or updates a list of backup job states to the persistent JSON file.
        /// </summary>
        /// <param name="states">The list of <see cref="RealTimeState"/> objects to persist.</param>
        public static void SaveStates(List<RealTimeState> states)
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    SetFilePath(Path.Combine(Directory.GetCurrentDirectory(), "state.json"));
                }

                var existingStates = LoadStates();

                foreach (var newState in states)
                {
                    if (newState != null && !string.IsNullOrEmpty(newState.Name))
                    {
                        newState.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        newState.SourceFilePath = newState.SourceFilePath ?? "";
                        newState.TargetFilePath = newState.TargetFilePath ?? "";
                        newState.State = newState.State ?? "INACTIVE";

                        var existingState = existingStates.FirstOrDefault(s => s.Name == newState.Name);
                        if (existingState != null)
                        {
                            // Update existing entry
                            existingState.Timestamp = newState.Timestamp;
                            existingState.State = newState.State;
                            existingState.SourceFilePath = newState.SourceFilePath;
                            existingState.TargetFilePath = newState.TargetFilePath;
                            existingState.TotalFilesToCopy = newState.TotalFilesToCopy;
                            existingState.TotalFilesSize = newState.TotalFilesSize;
                            existingState.NbFilesLeftToDo = newState.NbFilesLeftToDo;
                            existingState.Progression = newState.Progression;
                        }
                        else
                        {
                            // Add new entry
                            existingStates.Add(newState);
                        }
                    }
                }

                if (existingStates.Count == 0) return;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(existingStates, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving state: {ex.Message}");
                Console.WriteLine($"Attempted path: {_stateFilePath}");
            }
        }

        /// <summary>
        /// Loads all backup job states from the persistent JSON file.
        /// </summary>
        /// <returns>A list of <see cref="RealTimeState"/> objects, or an empty list if not found.</returns>
        public static List<RealTimeState> LoadStates()
        {
            if (string.IsNullOrEmpty(_stateFilePath))
            {
                return new List<RealTimeState>();
            }

            if (File.Exists(_stateFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_stateFilePath);
                    var states = JsonSerializer.Deserialize<List<RealTimeState>>(json) ?? new List<RealTimeState>();

                    return states.Where(s => s != null && !string.IsNullOrEmpty(s.Name)).ToList();
                }
                catch
                {
                    return new List<RealTimeState>();
                }
            }
            return new List<RealTimeState>();
        }

        /// <summary>
        /// Removes a specific backup job's state from the JSON file based on its name.
        /// </summary>
        /// <param name="saveName">The name of the backup job to remove.</param>
        public static void RemoveState(string saveName)
        {
            try
            {
                var existingStates = LoadStates();
                var stateToRemove = existingStates.FirstOrDefault(s => s.Name == saveName);

                if (stateToRemove != null)
                {
                    existingStates.Remove(stateToRemove);

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };

                    string json = JsonSerializer.Serialize(existingStates, options);
                    File.WriteAllText(_stateFilePath, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while removing state: {ex.Message}");
            }
        }
    }
}