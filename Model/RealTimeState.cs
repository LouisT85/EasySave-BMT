using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents the serialized real-time state for a backup job.
    /// </summary>
    public class RealTimeState
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static string _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");

        /// <summary>
        /// Gets or sets the backup job name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the state timestamp.
        /// </summary>
        public string Timestamp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current source file path.
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current destination file path.
        /// </summary>
        public string TargetFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the backup state label.
        /// </summary>
        public string State { get; set; } = "INACTIVE";

        /// <summary>
        /// Gets or sets the total files to copy.
        /// </summary>
        public int TotalFilesToCopy { get; set; }

        /// <summary>
        /// Gets or sets the total size of files to copy.
        /// </summary>
        public long TotalFilesSize { get; set; }

        /// <summary>
        /// Gets or sets the remaining number of files.
        /// </summary>
        public int NbFilesLeftToDo { get; set; }

        /// <summary>
        /// Gets or sets the progress percentage.
        /// </summary>
        public int Progression { get; set; }

        /// <summary>
        /// Configures the state file path.
        /// </summary>
        /// <param name="filePath">A directory or full file path.</param>
        public static void SetFilePath(string filePath)
        {
            _stateFilePath = ResolveStateFilePath(filePath);
            EnsureDirectoryExists(_stateFilePath);
        }

        /// <summary>
        /// Saves or updates state entries in the state file.
        /// </summary>
        /// <param name="states">States to persist.</param>
        public static void SaveStates(List<RealTimeState> states)
        {
            if (states is null || states.Count == 0)
            {
                return;
            }

            try
            {
                EnsureDirectoryExists(_stateFilePath);
                var existingStates = LoadStates();

                foreach (var incoming in states.Where(s => s is not null && !string.IsNullOrWhiteSpace(s.Name)))
                {
                    incoming.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    incoming.SourceFilePath ??= string.Empty;
                    incoming.TargetFilePath ??= string.Empty;
                    incoming.State ??= "INACTIVE";

                    var existing = existingStates.FirstOrDefault(s => s.Name == incoming.Name);
                    if (existing is null)
                    {
                        existingStates.Add(incoming);
                        continue;
                    }

                    existing.Timestamp = incoming.Timestamp;
                    existing.State = incoming.State;
                    existing.SourceFilePath = incoming.SourceFilePath;
                    existing.TargetFilePath = incoming.TargetFilePath;
                    existing.TotalFilesToCopy = incoming.TotalFilesToCopy;
                    existing.TotalFilesSize = incoming.TotalFilesSize;
                    existing.NbFilesLeftToDo = incoming.NbFilesLeftToDo;
                    existing.Progression = incoming.Progression;
                }

                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(existingStates, SerializerOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving state: {ex.Message}");
                Console.WriteLine($"Attempted path: {_stateFilePath}");
            }
        }

        /// <summary>
        /// Loads all states from disk.
        /// </summary>
        /// <returns>A list of persisted states.</returns>
        public static List<RealTimeState> LoadStates()
        {
            if (!File.Exists(_stateFilePath))
            {
                return new List<RealTimeState>();
            }

            try
            {
                string json = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<List<RealTimeState>>(json) ?? new List<RealTimeState>();
            }
            catch
            {
                return new List<RealTimeState>();
            }
        }

        /// <summary>
        /// Removes a state by backup name.
        /// </summary>
        /// <param name="saveName">The backup name to remove.</param>
        public static void RemoveState(string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName))
            {
                return;
            }

            try
            {
                var existingStates = LoadStates();
                existingStates.RemoveAll(s => string.Equals(s.Name, saveName, StringComparison.Ordinal));
                File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(existingStates, SerializerOptions));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while removing state: {ex.Message}");
            }
        }

        private static string ResolveStateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "state.json");
            }

            if (Directory.Exists(filePath) || !Path.HasExtension(filePath))
            {
                return Path.Combine(filePath, "state.json");
            }

            return filePath;
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
