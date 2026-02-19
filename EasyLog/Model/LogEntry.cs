using System;

namespace EasyLog.Models
{
    /// <summary>
    /// Represents a single log entry for a file transfer operation.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the operation.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the backup job name.
        /// </summary>
        public string BackupName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source file path.
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination file path.
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the transfer duration in milliseconds.
        /// A negative value indicates an error.
        /// </summary>
        public long TransferTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the encryption duration in milliseconds.
        /// <c>0</c> means no encryption, a negative value is an error code.
        /// </summary>
        public long EncryptionTimeMs { get; set; }
    }
}
