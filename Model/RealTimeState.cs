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
                            existingStates.Add(newState);
                        }
                    }
                }

                if (existingStates.Count == 0)
                {
                    return;
                }

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
                Console.WriteLine($"Erreur lors de la suppression de l'état: {ex.Message}");
            }
        }
    }
}
