namespace easySave_BMT.Model_
{
    /// <summary>
    /// Describes what happened during file encryption for a copied file.
    /// </summary>
    public enum EncryptionAction
    {
        /// <summary>
        /// No encryption action was executed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The file was encrypted.
        /// </summary>
        Encrypted = 1,

        /// <summary>
        /// Encryption was skipped because the file was already encrypted.
        /// </summary>
        SkippedAlreadyEncrypted = 2
    }
}
