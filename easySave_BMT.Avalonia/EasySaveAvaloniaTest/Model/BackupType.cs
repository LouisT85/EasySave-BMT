namespace easySave_BMT.Model_
{
    /// <summary>
    /// Defines the supported backup strategies for the application.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// A full backup copies all selected files from the source to the destination, 
        /// regardless of whether they have changed.
        /// </summary>
        FULL,

        /// <summary>
        /// A differential backup only copies files that have been created or modified 
        /// since the last full backup.
        /// </summary>
        DIFFERENTIAL,

        NONE
    }
}