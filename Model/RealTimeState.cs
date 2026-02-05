using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace easySave_BMT.Model_
{
    public class RealTimeState
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public string SourceFilePath { get; set; }
        public string TargetFilePath { get; set; }
        public string State { get; set; }
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int NbFilesLeftToDo { get; set; }
        public int Progression { get; set; }
        
        private static string _stateFilePath;

        public static void SetFilePath(string filePath)
        {
            _stateFilePath = filePath;
            
            if (!string.IsNullOrEmpty(_stateFilePath))
            {
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
                        Console.WriteLine($"Avertissement: Impossible de créer le répertoire '{directory}': {ex.Message}");
                        _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
                    }
                }
            }
            else
            {
                _stateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "state.json");
            }
        }

        public static void SaveStates(List<RealTimeState> states)
        {
            try
            {
                if (string.IsNullOrEmpty(_stateFilePath))
                {
                    SetFilePath(Path.Combine(Directory.GetCurrentDirectory(), "state.json"));
                }
                
                var validStates = new List<RealTimeState>();
                foreach (var state in states)
                {
                    if (state != null && !string.IsNullOrEmpty(state.Name))
                    {
                        state.Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        state.SourceFilePath = state.SourceFilePath ?? "";
                        state.TargetFilePath = state.TargetFilePath ?? "";
                        state.State = state.State ?? "END";
                        
                        validStates.Add(state);
                    }
                }
                
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
                Console.WriteLine($"Erreur de sauvegarde d'état: {ex.Message}");
                Console.WriteLine($"Chemin tenté: {_stateFilePath}");
            }
        }

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
    }
}