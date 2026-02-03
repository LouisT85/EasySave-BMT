using System.Text.Json;
using EasyLog.Models;

namespace EasyLog
{
    public class EasyLogger
    {
        private readonly string _logDirectory;

        public EasyLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void Write(LogEntry entry)
        {
            string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
            string filePath = Path.Combine(_logDirectory, fileName);

            string json = JsonSerializer.Serialize(
                entry,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.AppendAllText(filePath, json + Environment.NewLine);
        }
    }
}
