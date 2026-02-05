using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace easySave_BMT.Model_
{

    /// Represents real-time state information for a backup job

    public class RealTimeState
    {

        /// Name of the save job

        public string Name { get; set; }
        

        /// Current source file path being processed

        public string SourceFilePath { get; set; }
        

        /// Current target file path

        public string TargetFilePath { get; set; }
        

        /// Current state (ACTIVE/END)

        public string State { get; set; }
        
 
        /// Total number of files to copy

        public int TotalFilesToCopy { get; set; }
        

        /// Total size of files to copy in bytes

        public long TotalFilesSize { get; set; }
        

        /// Number of files left to process

        public int NbFilesLeftToDo { get; set; }
        

        /// Progress percentage (0-100)

        public int Progression { get; set; }
        
        private static string _stateFilePath;
        

        /// Sets the path for the state file


        public static void SetFilePath(string filePath)
        {
            _stateFilePath = filePath;
            
            // Validate and ensure we have a proper filename
            if (!string.IsNullOrEmpty(_stateFilePath))
            {
                // If only a directory path is provided (like Desktop), append a filename
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
                        // Fallback to application directory
                        _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
                    }
                }
            }
            else
            {
                // Default fallback
                _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
            }
        }
        

        /// Saves all states to the state file

        public static void SaveStates(List<RealTimeState> states)
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    SetFilePath(Path.Combine(Directory.GetCurrentDirectory(), "state.json"));
                }
                
                // Filter out null entries and ensure all properties have values
                var validStates = new List<RealTimeState>();
                foreach (var state in states)
                {
                    if (state != null && !string.IsNullOrEmpty(state.Name))
                    {
                        // Ensure all properties have values
                        state.SourceFilePath = state.SourceFilePath ?? "";
                        state.TargetFilePath = state.TargetFilePath ?? "";
                        state.State = state.State ?? "END";
                        
                        validStates.Add(state);
                    }
                }
                
                // If no valid states, don't write anything
                if (validStates.Count == 0)
                {
                    return;
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                string json = JsonSerializer.Serialize(validStates, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"State save error: {ex.Message}");
                Console.WriteLine($"Attempted path: {_stateFilePath}");
            }
        }
        

        /// Loads states from the state file

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
                    
                    // Filter out null entries
                    return states.Where(s => s != null && !string.IsNullOrEmpty(s.Name)).ToList();
                }
                catch
                {
                    return new List<RealTimeState>();
                }
            }
            return new List<RealTimeState>();
        }
    }
}