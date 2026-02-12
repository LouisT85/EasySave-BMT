namespace easySave_BMT.View_
{
    public interface IProgressObserver
    {
        void OnProgressUpdate(string backupName, int filesLeft, long sizeLeft, long currentFileSize, int percent);
        void OnBackupComplete(string backupName, double transferTime);
        void OnFileError(string fileName);
    }
}
