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
            Directory.CreateDirectory(logDirectory);
        }

        public void Write(LogEntry entry)
        {
            string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
            string filePath = Path.Combine(_logDirectory, fileName);

            // Format JSON avec date en français
            var logObject = new
            {
                Name = entry.BackupName,
                FileSource = entry.SourcePath,
                FileTarget = entry.DestinationPath,
                FileSize = entry.FileSize,
                FileTransferTime = entry.TransferTimeMs,
                time = entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss")
            };

            string json = JsonSerializer.Serialize(
                logObject,
                new JsonSerializerOptions { WriteIndented = true }
            );

            // Si le fichier existe déjà, ajouter une virgule et une nouvelle ligne
            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                // Lire le contenu existant
                string existingContent = File.ReadAllText(filePath).Trim();
                
                // Si le contenu n'a pas de crochet ouvrant, on en ajoute
                if (!existingContent.StartsWith("["))
                {
                    existingContent = $"[{existingContent}";
                }
                
                // Si le dernier caractère n'est pas un crochet fermant, on ajoute la virgule
                if (!existingContent.EndsWith("]"))
                {
                    existingContent = existingContent.TrimEnd(',', '\n', '\r', ' ');
                    existingContent += ",\n";
                }
                else
                {
                    // Retirer le crochet fermant et ajouter une virgule
                    existingContent = existingContent.TrimEnd(']');
                    existingContent += ",\n";
                }
                
                // Ajouter la nouvelle entrée et fermer le tableau
                string newContent = existingContent + json + "\n]";
                File.WriteAllText(filePath, newContent);
            }
            else
            {
                // Nouveau fichier
                string newContent = $"[{json}\n]";
                File.WriteAllText(filePath, newContent);
            }
        }
    }
}