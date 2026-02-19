namespace easySave_BMT.Model_
{
    /// <summary>
    /// Defines supported backup strategies.
    /// </summary>
    public enum BackupType
    {
        /// <summary>
        /// Copies every file from source to destination.
        /// </summary>
        FULL,

        /// <summary>
        /// Copies only files changed since the last full backup.
        /// </summary>
        DIFFERENTIAL,

        /// <summary>
        /// Represents no selected backup type.
        /// </summary>
        NONE
    }
}
