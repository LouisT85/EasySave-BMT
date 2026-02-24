using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using EasyLog.Models;

namespace EasyLog
{
    /// <summary>
    /// Writes backup logs locally (XML/JSON), to a centralized HTTP service, or both.
    /// </summary>
    public class EasyLogger : IDisposable
    {
        public enum LogFormat
        {
            XML,
            JSON
        }

        public enum DestinationMode
        {
            LocalOnly,
            CentralizedOnly,
            LocalAndCentralized
        }

        private readonly string _logDirectory;
        private readonly LogFormat _format;
        private readonly DestinationMode _destinationMode;
        private readonly Uri? _centralizedEndpoint;
        private readonly HttpClient? _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly object _writeLock = new();

        public EasyLogger(string logDirectory)
            : this(logDirectory, LogFormat.XML, DestinationMode.LocalOnly, centralizedEndpoint: null)
        {
        }

        public EasyLogger(string logDirectory, LogFormat format)
            : this(logDirectory, format, DestinationMode.LocalOnly, centralizedEndpoint: null)
        {
        }

        public EasyLogger(
            string logDirectory,
            LogFormat format,
            DestinationMode destinationMode,
            string? centralizedEndpoint,
            HttpClient? httpClient = null)
        {
            _logDirectory = logDirectory ?? string.Empty;
            _format = format;
            _destinationMode = destinationMode;

            if (ShouldWriteLocal())
            {
                Directory.CreateDirectory(_logDirectory);
            }

            if (ShouldSendCentralized())
            {
                if (string.IsNullOrWhiteSpace(centralizedEndpoint) ||
                    !Uri.TryCreate(centralizedEndpoint.Trim(), UriKind.Absolute, out Uri? parsedEndpoint))
                {
                    throw new ArgumentException(
                        "A valid centralized endpoint URL is required when centralized logging is enabled.",
                        nameof(centralizedEndpoint));
                }

                _centralizedEndpoint = parsedEndpoint;
                _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                _ownsHttpClient = httpClient is null;
            }
        }

        public void Write(LogEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            EnsureEntryIdentity(entry);

            if (ShouldWriteLocal())
            {
                lock (_writeLock)
                {
                    WriteLocal(entry);
                }
            }

            if (ShouldSendCentralized())
            {
                SendToCentralized(entry);
            }
        }

        public void Dispose()
        {
            if (_ownsHttpClient)
            {
                _httpClient?.Dispose();
            }
        }

        private bool ShouldWriteLocal()
        {
            return _destinationMode == DestinationMode.LocalOnly ||
                   _destinationMode == DestinationMode.LocalAndCentralized;
        }

        private bool ShouldSendCentralized()
        {
            return _destinationMode == DestinationMode.CentralizedOnly ||
                   _destinationMode == DestinationMode.LocalAndCentralized;
        }

        private static void EnsureEntryIdentity(LogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.MachineName))
            {
                entry.MachineName = Environment.MachineName;
            }

            if (string.IsNullOrWhiteSpace(entry.UserName))
            {
                entry.UserName = Environment.UserName;
            }
        }

        private void WriteLocal(LogEntry entry)
        {
            string fileName = $"{DateTime.Now:yyyy-MM-dd}." + (_format == LogFormat.XML ? "xml" : "json");
            string filePath = Path.Combine(_logDirectory, fileName);

            if (_format == LogFormat.JSON)
            {
                WriteLocalJson(filePath, entry);
            }
            else
            {
                WriteLocalXml(filePath, entry);
            }
        }

        private static void WriteLocalJson(string filePath, LogEntry entry)
        {
            List<object> entries = new List<object>();

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                try
                {
                    string existingRaw = File.ReadAllText(filePath);
                    using var existingDoc = JsonDocument.Parse(existingRaw);
                    if (existingDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var existing in existingDoc.RootElement.EnumerateArray())
                        {
                            entries.Add(existing.Clone());
                        }
                    }
                }
                catch
                {
                    entries.Clear();
                }
            }

            entries.Add(BuildLegacyLocalJsonObject(entry));

            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private static object BuildLegacyLocalJsonObject(LogEntry entry)
        {
            return new
            {
                Name = entry.BackupName,
                FileSource = entry.SourcePath,
                FileTarget = entry.DestinationPath,
                FileSize = entry.FileSize,
                FileTransferTime = entry.TransferTimeMs,
                EncryptionTime = entry.EncryptionTimeMs,
                MachineName = entry.MachineName,
                UserName = entry.UserName,
                time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        private static void WriteLocalXml(string filePath, LogEntry entry)
        {
            XElement logElement = new XElement("Log",
                new XElement("Name", entry.BackupName),
                new XElement("FileSource", entry.SourcePath),
                new XElement("FileTarget", entry.DestinationPath),
                new XElement("FileSize", entry.FileSize),
                new XElement("FileTransferTime", entry.TransferTimeMs),
                new XElement("EncryptionTime", entry.EncryptionTimeMs),
                new XElement("MachineName", entry.MachineName),
                new XElement("UserName", entry.UserName),
                new XElement("Time", entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"))
            );

            XDocument doc;
            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                try
                {
                    doc = XDocument.Load(filePath);
                }
                catch
                {
                    doc = new XDocument(new XElement("Logs"));
                }
            }
            else
            {
                doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("Logs"));
            }

            var root = doc.Element("Logs");
            if (root is null)
            {
                root = new XElement("Logs");
                doc.Add(root);
            }

            root.Add(logElement);
            doc.Save(filePath);
        }

        private void SendToCentralized(LogEntry entry)
        {
            if (_centralizedEndpoint is null || _httpClient is null)
            {
                throw new InvalidOperationException("Centralized logger is not initialized.");
            }

            var payload = new
            {
                Timestamp = entry.Timestamp,
                BackupName = entry.BackupName,
                SourcePath = entry.SourcePath,
                DestinationPath = entry.DestinationPath,
                FileSize = entry.FileSize,
                TransferTimeMs = entry.TransferTimeMs,
                EncryptionTimeMs = entry.EncryptionTimeMs,
                MachineName = entry.MachineName,
                UserName = entry.UserName
            };

            string body = JsonSerializer.Serialize(payload);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = _httpClient
                .PostAsync(_centralizedEndpoint, content)
                .GetAwaiter()
                .GetResult();

            response.EnsureSuccessStatusCode();
        }
    }
}
