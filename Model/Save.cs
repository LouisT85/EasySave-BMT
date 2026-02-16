using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents a backup job configuration, containing the source and destination paths,
    /// the type of backup, and its current execution state.
    /// </summary>
    public class Save
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _uiProgressPercent;

        /// <summary>
        /// UI-only progress (0-100) for the GUI list. Not persisted to BackupSave.json.
        /// </summary>
        [JsonIgnore]
        public int UiProgressPercent
        {
            get => _uiProgressPercent;
            set
            {
                if (_uiProgressPercent == value) return;
                _uiProgressPercent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>The unique name assigned to the backup job.</summary>
        public string name { get; set; }

        /// <summary>The source directory path to be backed up.</summary>
        public string src { get; set; }

        /// <summary>The destination directory path where the backup will be stored.</summary>
        public string dst { get; set; }

        /// <summary>The type of backup to perform (e.g., Full or Differential).</summary>
        public BackupType backupType { get; set; }

        /// <summary>
        /// The current dynamic state of the job, tracking progress and file details 
        /// during execution.
        /// </summary>
        public State state { get; set; }

        /// <summary>The timestamp of the last time this backup job was successfully completed.</summary>
        public string lastBackupDate { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="Save"/> class.
        /// </summary>
        public Save()
        {
            this.state = null;
            this.lastBackupDate = "";
            this.UiProgressPercent = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Save"/> class with specific configuration details.
        /// </summary>
        /// <param name="name">The name of the backup task.</param>
        /// <param name="src">The source folder path.</param>
        /// <param name="dst">The destination folder path.</param>
        /// <param name="backupType">The selected <see cref="BackupType"/> for this job.</param>
        public Save(string name, string src, string dst, BackupType backupType)
        {
            this.name = name;
            this.src = src;
            this.dst = dst;
            this.backupType = backupType;
            this.state = null;
            this.lastBackupDate = "";
            this.UiProgressPercent = 0;
        }
    }
}
