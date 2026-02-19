namespace easySave_BMT.View_
{
    /// <summary>
    /// Defines progress callbacks for backup execution observers.
    /// </summary>
    public interface IProgressObserver
    {
        /// <summary>
        /// Notifies about backup progress updates.
        /// </summary>
        void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent);

        /// <summary>
        /// Notifies when a backup completes.
        /// </summary>
        void OnBackupComplete(string backupName, double transferTime);

        /// <summary>
        /// Notifies when a file transfer fails.
        /// </summary>
        void OnFileError(string fileName);
    }
}
