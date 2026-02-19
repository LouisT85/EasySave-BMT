using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using EasyLog.Models;

namespace EasyLog
{
    /// <summary>
    /// Writes daily backup logs in JSON or XML format.
    /// </summary>
    public class EasyLogger
    {
        private readonly string _logDirectory;
        private readonly LogFormat _format;

        /// <summary>
        /// Supported log serialization formats.
        /// </summary>
        public enum LogFormat
        {
            /// <summary>
            /// XML log output.
            /// </summary>
            XML,

            /// <summary>
            /// JSON log output.
            /// </summary>
            JSON
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyLogger"/> class using XML output.
        /// </summary>
        /// <param name="logDirectory">The directory where logs are written.</param>
        public EasyLogger(string logDirectory)
            : this(logDirectory, LogFormat.XML)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EasyLogger"/> class.
        /// </summary>
        /// <param name="logDirectory">The directory where logs are written.</param>
        /// <param name="format">The output log format.</param>
        public EasyLogger(string logDirectory, LogFormat format)
        {
            _logDirectory = logDirectory;
            _format = format;
            Directory.CreateDirectory(logDirectory);
        }

        /// <summary>
        /// Appends one log entry to the current daily log file.
        /// </summary>
        /// <param name="entry">The log entry to write.</param>
        public void Write(LogEntry entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string filePath = GetDailyFilePath();

            if (_format == LogFormat.JSON)
            {
                WriteJson(filePath, entry);
                return;
            }

            WriteXml(filePath, entry);
        }

        private string GetDailyFilePath()
        {
            string extension = _format == LogFormat.XML ? "xml" : "json";
            string fileName = $"{DateTime.Now:yyyy-MM-dd}.{extension}";
            return Path.Combine(_logDirectory, fileName);
        }

        private static object CreateJsonRecord(LogEntry entry)
        {
            return new
            {
                Name = entry.BackupName,
                FileSource = entry.SourcePath,
                FileTarget = entry.DestinationPath,
                FileSize = entry.FileSize,
                FileTransferTime = entry.TransferTimeMs,
                EncryptionTime = entry.EncryptionTimeMs,
                Time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")
            };
        }

        private static XElement CreateXmlRecord(LogEntry entry)
        {
            return new XElement("Log",
                new XElement("Name", entry.BackupName),
                new XElement("FileSource", entry.SourcePath),
                new XElement("FileTarget", entry.DestinationPath),
                new XElement("FileSize", entry.FileSize),
                new XElement("FileTransferTime", entry.TransferTimeMs),
                new XElement("EncryptionTime", entry.EncryptionTimeMs),
                new XElement("Time", entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")));
        }

        private static void WriteJson(string filePath, LogEntry entry)
        {
            List<object> records = new();

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                try
                {
                    string existing = File.ReadAllText(filePath);
                    var parsed = JsonSerializer.Deserialize<List<JsonElement>>(existing);
                    if (parsed is not null)
                    {
                        records.AddRange(parsed);
                    }
                }
                catch
                {
                    records = new List<object>();
                }
            }

            records.Add(CreateJsonRecord(entry));
            File.WriteAllText(filePath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void WriteXml(string filePath, LogEntry entry)
        {
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
                doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("Logs"));
            }

            XElement root = doc.Element("Logs") ?? new XElement("Logs");
            if (doc.Element("Logs") is null)
            {
                doc.Add(root);
            }

            root.Add(CreateXmlRecord(entry));
            doc.Save(filePath);
        }
    }
}
