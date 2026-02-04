using System.Text.Json;

namespace easySave_BMT.Model_
{
    public class Config
    {
        public string LogDirectory { get; set; }
        public string StateFilePath { get; set; }
        public string Language { get; set; } = "fr";
        
        private static readonly string ConfigPath = "./config.json";
        
        public Config()
        {
            // Valeurs par défaut
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string easySavePath = Path.Combine(appDataPath, "EasySave");
            
            LogDirectory = Path.Combine(easySavePath, "Logs");
            StateFilePath = Path.Combine(easySavePath, "state.json");
        }
        
        public static Config Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<Config>(json) ?? new Config();
                }
                catch
                {
                    return new Config();
                }
            }
            return new Config();
        }
        
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur sauvegarde config: {ex.Message}");
            }
        }
        
        public void UpdateFromUserInput(string logDir, string statePath, string lang)
        {
            if (!string.IsNullOrWhiteSpace(logDir))
                LogDirectory = logDir;
            
            if (!string.IsNullOrWhiteSpace(statePath))
                StateFilePath = statePath;
            
            if (!string.IsNullOrWhiteSpace(lang))
                Language = lang;
            
            Save();
        }
    }
}