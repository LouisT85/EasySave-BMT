using System.Text.Json;
using System.IO;
using System;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Manages the application settings, including file paths for logs and states, 
    /// as well as language preferences. Handles JSON serialization for persistence.
    /// </summary>
    public class Config
    {
        /// <summary>The directory where backup log files are stored.</summary>
        public string LogDirectory { get; set; }

        /// <summary>The full path to the real-time state JSON file.</summary>
        public string StateFilePath { get; set; }

        /// <summary>The preferred UI language code (e.g., "en", "fr"). Defaults to "fr".</summary>
        public string Language { get; set; } = "fr";

        /// <summary>Preferred log file format: "XML" or "JSON". Defaults to XML to satisfy client requirement.</summary>
        public string LogFormat { get; set; } = "XML";

        /// <summary>Enable file encryption using CryptoSoft after copy.</summary>
        public bool EnableEncryption { get; set; } = false;

        /// <summary>
        /// File extensions eligible for encryption (e.g., [".txt", ".pdf"]).
        /// Comparison is case-insensitive.
        /// </summary>
        public System.Collections.Generic.List<string> EncryptionExtensions { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Optional explicit path to CryptoSoft executable. If empty, EasySave will try to auto-detect it.
        /// </summary>
        public string CryptoSoftPath { get; set; } = "";

        /// <summary>
        /// Optional "business software" process name (or exe path). When running, backups must be blocked/stopped.
        /// Example for demos: "calc" or "calc.exe".
        /// </summary>
        public string BusinessSoftware { get; set; } = "";

        /// <summary>The relative path to the configuration file itself.</summary>
        private static readonly string ConfigPath = "./config.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class with default system paths.
        /// Defaults are set within the CommonApplicationData folder.
        /// </summary>
        public Config()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string easySavePath = Path.Combine(appDataPath, "EasySave");

            LogDirectory = Path.Combine(easySavePath, "Logs");
            StateFilePath = Path.Combine(easySavePath, "state.json");
        }

        /// <summary>
        /// Loads the configuration from the local JSON file. 
        /// If the file does not exist or is corrupted, returns a new <see cref="Config"/> with default values.
        /// </summary>
        /// <returns>A populated <see cref="Config"/> object.</returns>
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
                    // Fallback to defaults on deserialization error
                    return new Config();
                }
            }
            return new Config();
        }

        /// <summary>
        /// Persists the current configuration settings to the config.json file.
        /// </summary>
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

        /// <summary>
        /// Updates the configuration properties based on user input and saves them immediately.
        /// </summary>
        /// <param name="logDir">New directory for logs (ignored if empty).</param>
        /// <param name="statePath">New path for the state file (ignored if empty).</param>
        /// <param name="lang">New language code (ignored if empty).</param>
        public void UpdateFromUserInput(
            string logDir,
            string statePath,
            string lang,
            bool? enableEncryption = null,
            System.Collections.Generic.List<string>? encryptionExtensions = null,
            string? cryptoSoftPath = null,
            string? businessSoftware = null)
        {
            if (!string.IsNullOrWhiteSpace(logDir))
                LogDirectory = logDir;

            if (!string.IsNullOrWhiteSpace(statePath))
                StateFilePath = statePath;

            if (!string.IsNullOrWhiteSpace(lang))
                Language = lang;

            if (enableEncryption.HasValue)
                EnableEncryption = enableEncryption.Value;

            if (encryptionExtensions is not null)
                EncryptionExtensions = encryptionExtensions;

            if (cryptoSoftPath is not null)
                CryptoSoftPath = cryptoSoftPath;

            if (businessSoftware is not null)
                BusinessSoftware = businessSoftware;

            Save();
        }
    }
}
