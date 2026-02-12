using System;

namespace EasyLog.Models
{
    /// <summary>
    /// Represents a single log entry for a file transfer operation.
    /// This data is persisted to provide a history of backup activities.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// The date and time when the file transfer operation occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The name of the backup job associated with this entry.
        /// </summary>
        public string BackupName { get; set; }

        /// <summary>
        /// The full source path of the file that was copied.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// The full destination path where the file was stored.
        /// </summary>
        public string DestinationPath { get; set; }

        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// The time taken to transfer the file in milliseconds. 
        /// A value of -1 typically indicates a failed transfer.
        /// </summary>
        public long TransferTimeMs { get; set; }

        /// <summary>
        /// The time taken to encrypt the file in milliseconds.
        /// 0 : no encryption
        /// >0 : encryption time in ms
        /// <0 : error code
        /// </summary>
        public long EncryptionTimeMs { get; set; }
    }
}