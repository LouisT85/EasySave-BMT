using System;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using EasyLog.Models;


namespace EasyLog
{
    /// <summary>
    /// EasyLogger is class for write backup logs in JSON files daily.
    /// Each day one file with array of log entries, format human readable.
    /// </summary>
    public class EasyLogger
    {
        /// <summary>
        /// The directory path where store log files.
        /// Created if not exist.
        /// </summary>
        private readonly string _logDirectory;


        /// <summary>
        /// Constructor initialize logger with log directory.
        /// Create directory if needed.
        /// </summary>
        /// <param name="logDirectory">Path for log files.</param>
        public enum LogFormat { XML, JSON }

        private readonly LogFormat _format = LogFormat.XML;
        private readonly object _writeLock = new();

        public EasyLogger(string logDirectory) : this(logDirectory, LogFormat.XML)
        {
        }

        public EasyLogger(string logDirectory, LogFormat format)
        {
            _logDirectory = logDirectory;
            _format = format;
            Directory.CreateDirectory(logDirectory);
        }


        /// <summary>
        /// Write one log entry to todays JSON file.
        /// Append to array if file exist, create new if first.
        /// Use french date format dd/MM/yyyy.
        /// </summary>
        /// <param name="entry">The LogEntry with backup details.</param>
        public void Write(LogEntry entry)
        {
            lock (_writeLock)
            {
                string fileName = $"{DateTime.Now:yyyy-MM-dd}." + (_format == LogFormat.XML ? "xml" : "json");
                string filePath = Path.Combine(_logDirectory, fileName);

                if (_format == LogFormat.JSON)
                {
                    // Format JSON with date in french style
                    var logObject = new
                    {
                        Name = entry.BackupName,
                        FileSource = entry.SourcePath,
                        FileTarget = entry.DestinationPath,
                        FileSize = entry.FileSize,
                        FileTransferTime = entry.TransferTimeMs,
                        EncryptionTime = entry.EncryptionTimeMs,
                        time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")
                    };

                    string json = JsonSerializer.Serialize(
                        logObject,
                        new JsonSerializerOptions { WriteIndented = true }
                    );

                    // If file exist and not empty, append with comma
                    if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                    {
                        // Read existing content
                        string existingContent = File.ReadAllText(filePath).Trim();

                        // If no open bracket, add it
                        if (!existingContent.StartsWith("["))
                        {
                            existingContent = $"[{existingContent}";
                        }

                        // Manage close bracket and add comma if need
                        if (!existingContent.EndsWith("]"))
                        {
                            existingContent = existingContent.TrimEnd(',', '\n', '\r', ' ');
                            existingContent += ",\n";
                        }
                        else
                        {
                            // Remove close ] and add comma
                            existingContent = existingContent.TrimEnd(']');
                            existingContent += ",\n";
                        }

                        // Add new entry and close array
                        string newContent = existingContent + json + "\n]";
                        File.WriteAllText(filePath, newContent);
                    }
                    else
                    {
                        // New file, create array with one entry
                        string newContent = $"[{json}\n]";
                        File.WriteAllText(filePath, newContent);
                    }
                }
                else
                {
                    // XML format: store as root <Logs><Log>...</Log></Logs>
                    XElement logElement = new XElement("Log",
                        new XElement("Name", entry.BackupName),
                        new XElement("FileSource", entry.SourcePath),
                        new XElement("FileTarget", entry.DestinationPath),
                        new XElement("FileSize", entry.FileSize),
                        new XElement("FileTransferTime", entry.TransferTimeMs),
                        new XElement("EncryptionTime", entry.EncryptionTimeMs),
                        new XElement("Time", entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"))
                    );

                    if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                    {
                        // Load existing XML and append
                        XDocument doc;
                        try
                        {
                            doc = XDocument.Load(filePath);
                        }
                        catch
                        {
                            // If file is corrupted or empty, create new document
                            doc = new XDocument(new XElement("Logs"));
                        }

                        var root = doc.Element("Logs");
                        if (root == null)
                        {
                            root = new XElement("Logs");
                            doc.Add(root);
                        }

                        root.Add(logElement);
                        doc.Save(filePath);
                    }
                    else
                    {
                        // Create new XML document
                        XDocument doc = new XDocument(
                            new XDeclaration("1.0", "utf-8", "yes"),
                            new XElement("Logs", logElement)
                        );
                        doc.Save(filePath);
                    }
                }
            }
        }
    }
}
