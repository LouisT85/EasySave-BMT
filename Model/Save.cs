using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents a backup job configuration and its runtime UI state.
    /// </summary>
    public class Save : INotifyPropertyChanged
    {
        private int _uiProgressPercent;

        /// <summary>
        /// Occurs when a bindable property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// UI-only progress (0-100) for the GUI list. Not persisted to <c>BackupSave.json</c>.
        /// </summary>
        [JsonIgnore]
        public int UiProgressPercent
        {
            get => _uiProgressPercent;
            set
            {
                if (_uiProgressPercent == value)
                {
                    return;
                }

                _uiProgressPercent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the backup job name.
        /// </summary>
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source directory path.
        /// </summary>
        public string src { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the destination directory path.
        /// </summary>
        public string dst { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the backup strategy type.
        /// </summary>
        public BackupType backupType { get; set; }

        /// <summary>
        /// Gets or sets the runtime state for the running backup.
        /// </summary>
        public State? state { get; set; }

        /// <summary>
        /// Gets or sets the last successful backup timestamp.
        /// </summary>
        public string lastBackupDate { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new empty instance of the <see cref="Save"/> class.
        /// </summary>
        public Save()
        {
            UiProgressPercent = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Save"/> class.
        /// </summary>
        /// <param name="name">The backup name.</param>
        /// <param name="src">The source directory path.</param>
        /// <param name="dst">The destination directory path.</param>
        /// <param name="backupType">The backup strategy type.</param>
        public Save(string name, string src, string dst, BackupType backupType)
        {
            this.name = name;
            this.src = src;
            this.dst = dst;
            this.backupType = backupType;
            UiProgressPercent = 0;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
