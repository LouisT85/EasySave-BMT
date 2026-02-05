using System.Text.Json;
using System.IO;

namespace easySave_BMT.Model_
{

    /// Manages application configuration settings

    public class Config
    {

        /// Directory where log files are stored

        public string LogDirectory { get; set; }
        

        /// Path to the real-time state file

        public string StateFilePath { get; set; }
        

        /// Application language (fr/en)

        public string Language { get; set; } = "fr";
        
        private static readonly string ConfigPath = "./config.json";
        

        /// Initializes configuration with default values

        public Config()
        {
            // Default values
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string easySavePath = Path.Combine(appDataPath, "EasySave");
            
            LogDirectory = Path.Combine(easySavePath, "Logs");
            StateFilePath = Path.Combine(easySavePath, "state.json");
        }
        

        /// Loads configuration from file or creates default

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
        

        /// Saves current configuration to file

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
                Console.WriteLine($"Configuration save error: {ex.Message}");
            }
        }
        

        /// Updates configuration from user input

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