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
        public const string LogDestinationModeLocalOnly = "LocalOnly";
        public const string LogDestinationModeCentralizedOnly = "CentralizedOnly";
        public const string LogDestinationModeLocalAndCentralized = "LocalAndCentralized";

        /// <summary>The directory where backup log files are stored.</summary>
        public string LogDirectory { get; set; }

        /// <summary>The full path to the real-time state JSON file.</summary>
        public string StateFilePath { get; set; }

        /// <summary>The preferred UI language code (e.g., "en", "fr"). Defaults to "fr".</summary>
        public string Language { get; set; } = "fr";

        /// <summary>Preferred log file format: "XML" or "JSON". Defaults to XML to satisfy client requirement.</summary>
        public string LogFormat { get; set; } = "XML";

        /// <summary>
        /// Log routing mode: LocalOnly, CentralizedOnly, or LocalAndCentralized.
        /// </summary>
        public string LogDestinationMode { get; set; } = LogDestinationModeLocalOnly;

        /// <summary>
        /// Centralized logging HTTP endpoint (example: http://localhost:8080/logs).
        /// </summary>
        public string CentralizedLogEndpoint { get; set; } = string.Empty;

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
        /// File extensions considered priority during copy (e.g., [".docx", ".xlsx"]).
        /// Comparison is case-insensitive.
        /// </summary>
        public System.Collections.Generic.List<string> PriorityExtensions { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Optional encryption key override for CryptoSoft.
        /// Supports plain text or hexadecimal values prefixed with "0x".
        /// </summary>
        public string CryptoSoftKey { get; set; } = "";

        /// <summary>
        /// Saved encryption keys that can be selected in the UI.
        /// </summary>
        public System.Collections.Generic.List<string> CryptoSoftSavedKeys { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// GUI trace entries for encryption key generation events.
        /// </summary>
        public System.Collections.Generic.List<string> EncryptionKeyCreationTrace { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// Optional business-software process patterns (separated by ';', ',', or newline).
        /// Each entry can be a process name, an .exe path, or a wildcard pattern (e.g. "calc;notepad;excel*").
        /// </summary>
        public string BusinessSoftware { get; set; } = "";

        /// <summary>
        /// Preferred UI theme: "auto" (system), "light", or "dark".
        /// </summary>
        public string ThemePreference { get; set; } = "auto";

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
                    Config loaded = JsonSerializer.Deserialize<Config>(json) ?? new Config();
                    loaded.Normalize();
                    Config defaults = new Config();
                    loaded.EncryptionExtensions ??= new System.Collections.Generic.List<string>();
                    loaded.PriorityExtensions ??= new System.Collections.Generic.List<string>();
                    loaded.EncryptionKeyCreationTrace ??= new System.Collections.Generic.List<string>();
                    loaded.CryptoSoftPath ??= "";
                    loaded.CryptoSoftKey ??= "";
                    loaded.CryptoSoftSavedKeys ??= new System.Collections.Generic.List<string>();
                    loaded.BusinessSoftware ??= "";
                    if (string.IsNullOrWhiteSpace(loaded.LogDirectory)) loaded.LogDirectory = defaults.LogDirectory;
                    if (string.IsNullOrWhiteSpace(loaded.StateFilePath)) loaded.StateFilePath = defaults.StateFilePath;
                    if (string.IsNullOrWhiteSpace(loaded.Language)) loaded.Language = "fr";
                    if (string.IsNullOrWhiteSpace(loaded.LogFormat)) loaded.LogFormat = "XML";
                    return loaded;
                }
                catch
                {
                    // Fallback to defaults on deserialization error
                    return new Config();
                }
            }
            return new Config();
        }

        public void Normalize()
        {
            LogDestinationMode = NormalizeLogDestinationMode(LogDestinationMode);
            CentralizedLogEndpoint = NormalizeCentralizedLogEndpoint(CentralizedLogEndpoint);
        }

        public static string NormalizeCentralizedLogEndpoint(string? endpoint)
        {
            string normalized = (endpoint ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
            {
                return normalized;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            var builder = new UriBuilder(uri);
            string path = builder.Path ?? string.Empty;

            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                // If only host:port is provided, target the default centralized logs endpoint.
                builder.Path = "/logs";
            }
            else if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
            {
                builder.Path = path.TrimEnd('/');
            }

            return builder.Uri.AbsoluteUri;
        }

        public static string NormalizeLogDestinationMode(string? mode)
        {
            string normalized = (mode ?? string.Empty).Trim();

            if (string.Equals(normalized, LogDestinationModeLocalOnly, StringComparison.OrdinalIgnoreCase))
            {
                return LogDestinationModeLocalOnly;
            }

            if (string.Equals(normalized, LogDestinationModeCentralizedOnly, StringComparison.OrdinalIgnoreCase))
            {
                return LogDestinationModeCentralizedOnly;
            }

            if (string.Equals(normalized, LogDestinationModeLocalAndCentralized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Both", StringComparison.OrdinalIgnoreCase))
            {
                return LogDestinationModeLocalAndCentralized;
            }

            if (string.Equals(normalized, "Local", StringComparison.OrdinalIgnoreCase))
            {
                return LogDestinationModeLocalOnly;
            }

            if (string.Equals(normalized, "Centralized", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "RemoteOnly", StringComparison.OrdinalIgnoreCase))
            {
                return LogDestinationModeCentralizedOnly;
            }

            return LogDestinationModeLocalOnly;
        }

        public static bool RequiresCentralizedEndpoint(string? mode)
        {
            string normalized = NormalizeLogDestinationMode(mode);
            return string.Equals(normalized, LogDestinationModeCentralizedOnly, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, LogDestinationModeLocalAndCentralized, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsLocalLoggingEnabled()
        {
            string mode = NormalizeLogDestinationMode(LogDestinationMode);
            return string.Equals(mode, LogDestinationModeLocalOnly, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, LogDestinationModeLocalAndCentralized, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsCentralizedLoggingEnabled()
        {
            string mode = NormalizeLogDestinationMode(LogDestinationMode);
            return string.Equals(mode, LogDestinationModeCentralizedOnly, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mode, LogDestinationModeLocalAndCentralized, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Persists the current configuration settings to the config.json file.
        /// </summary>
        public void Save()
        {
            try
            {
                Normalize();
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
            string? logDir,
            string? statePath,
            string? lang,
            bool? enableEncryption = null,
            System.Collections.Generic.List<string>? encryptionExtensions = null,
            System.Collections.Generic.List<string>? priorityExtensions = null,
            string? cryptoSoftPath = null,
            string? cryptoSoftKey = null,
            System.Collections.Generic.List<string>? cryptoSoftSavedKeys = null,
            System.Collections.Generic.List<string>? encryptionKeyCreationTrace = null,
            string? businessSoftware = null,
            string? themePreference = null,
            string? logDestinationMode = null,
            string? centralizedLogEndpoint = null)
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

            if (priorityExtensions is not null)
                PriorityExtensions = priorityExtensions;

            if (cryptoSoftPath is not null)
                CryptoSoftPath = cryptoSoftPath;

            if (cryptoSoftKey is not null)
                CryptoSoftKey = cryptoSoftKey;

            if (cryptoSoftSavedKeys is not null)
                CryptoSoftSavedKeys = cryptoSoftSavedKeys;

            if (encryptionKeyCreationTrace is not null)
                EncryptionKeyCreationTrace = encryptionKeyCreationTrace;

            if (businessSoftware is not null)
                BusinessSoftware = businessSoftware;

            if (themePreference is not null)
            {
                string normalizedTheme = (themePreference ?? "").Trim().ToLowerInvariant();
                ThemePreference = normalizedTheme switch
                {
                    "light" => "light",
                    "dark" => "dark",
                    _ => "auto"
                };
            }

            if (logDestinationMode is not null)
                LogDestinationMode = NormalizeLogDestinationMode(logDestinationMode);

            if (centralizedLogEndpoint is not null)
                CentralizedLogEndpoint = NormalizeCentralizedLogEndpoint(centralizedLogEndpoint);

            Normalize();

            Save();
        }
    }
}
