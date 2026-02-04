using System.Text.Json;
using System.Collections.Generic;
using System.IO;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents real-time state information for a backup job
    /// </summary>
    public class RealTimeState
    {
        /// <summary>
        /// Name of the save job
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Current source file path being processed
        /// </summary>
        public string SourceFilePath { get; set; }
        
        /// <summary>
        /// Current target file path
        /// </summary>
        public string TargetFilePath { get; set; }
        
        /// <summary>
        /// Current state (ACTIVE/END)
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// Total number of files to copy
        /// </summary>
        public int TotalFilesToCopy { get; set; }
        
        /// <summary>
        /// Total size of files to copy in bytes
        /// </summary>
        public long TotalFilesSize { get; set; }
        
        /// <summary>
        /// Number of files left to process
        /// </summary>
        public int NbFilesLeftToDo { get; set; }
        
        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int Progression { get; set; }
        
        private static string _stateFilePath;
        
        /// <summary>
        /// Sets the path for the state file
        /// </summary>
        /// <param name="filePath">Path to the state file</param>
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
            
            Console.WriteLine($"State file will be saved to: {_stateFilePath}");
        }
        
        /// <summary>
        /// Saves all states to the state file
        /// </summary>
        /// <param name="states">List of states to save</param>
        public static void SaveStates(List<RealTimeState> states)
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    SetFilePath(Path.Combine(Directory.GetCurrentDirectory(), "state.json"));
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(states, options);
                File.WriteAllText(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"State save error: {ex.Message}");
                Console.WriteLine($"Attempted path: {_stateFilePath}");
                
                // Try to save to a different location
                try
                {
                    string fallbackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "state.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath));
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(states, options);
                    File.WriteAllText(fallbackPath, json);
                    Console.WriteLine($"State saved to fallback location: {fallbackPath}");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Fallback save also failed: {fallbackEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// Loads states from the state file
        /// </summary>
        /// <returns>List of loaded states or empty list</returns>
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
                    return JsonSerializer.Deserialize<List<RealTimeState>>(json) ?? new List<RealTimeState>();
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