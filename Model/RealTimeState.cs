using System.Text.Json;

namespace easySave_BMT.Model_
{
    public class RealTimeState
    {
        public string Name { get; set; }
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
            // Créer le dossier parent si nécessaire
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath) ?? "./");
        }
        
        public static void SaveStates(List<RealTimeState> states)
        {
            try
            {
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
                Console.WriteLine($"Erreur sauvegarde état: {ex.Message}");
            }
        }
        
        public static List<RealTimeState> LoadStates()
        {
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