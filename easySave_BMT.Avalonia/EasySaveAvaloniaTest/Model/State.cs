using System;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents the real-time execution state of a backup job, 
    /// tracking progress, file counts, and current file paths.
    /// </summary>
    public class State
    {
        /// <summary>Total number of files to be processed in the backup job.</summary>
        public int totalFile { get; set; }

        /// <summary>Total size in bytes of all files to be processed.</summary>
        public long totalSize { get; set; }

        /// <summary>The current progress percentage (0-100).</summary>
        public int progress { get; set; }

        /// <summary>Number of files remaining to be processed.</summary>
        public int nbFileLeft { get; set; }

        /// <summary>Remaining size in bytes to be processed.</summary>
        public long leftSize { get; set; }

        /// <summary>The source path of the file currently being processed.</summary>
        public string currentPathSrc { get; set; }

        /// <summary>The destination path of the file currently being processed.</summary>
        public string currentPathDest { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="State"/> class.
        /// </summary>
        public State() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class with initial job details.
        /// </summary>
        /// <param name="totalFile">Total files in the job.</param>
        /// <param name="totalSize">Total size of the job in bytes.</param>
        /// <param name="currentPathSrc">Initial source path.</param>
        /// <param name="currentPathDest">Initial destination path.</param>
        public State(int totalFile, long totalSize, string currentPathSrc, string currentPathDest)
        {
            this.progress = 0;
            this.totalFile = totalFile;
            this.totalSize = totalSize;
            this.currentPathSrc = currentPathSrc ?? "";
            this.currentPathDest = currentPathDest ?? "";
        }

        /// <summary>
        /// Updates the current execution statistics and file paths.
        /// </summary>
        /// <param name="progress">Current percentage of completion.</param>
        /// <param name="nbFileLeft">Number of files remaining.</param>
        /// <param name="leftSize">Size remaining in bytes.</param>
        /// <param name="currSrcPath">Current source file path.</param>
        /// <param name="currDestPath">Current destination file path.</param>
        public void UpdateState(int progress, int nbFileLeft, long leftSize, string currSrcPath, string currDestPath)
        {
            this.progress = progress;
            this.nbFileLeft = nbFileLeft;
            this.leftSize = leftSize;
            this.currentPathSrc = currSrcPath ?? "";
            this.currentPathDest = currDestPath ?? "";
        }

        /// <summary>
        /// Converts the current state into a <see cref="RealTimeState"/> object for logging or export.
        /// </summary>
        /// <param name="saveName">The name of the backup job.</param>
        /// <returns>A populated <see cref="RealTimeState"/> object with an "ACTIVE" status.</returns>
        public RealTimeState ToRealTimeState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? "",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "ACTIVE",
                SourceFilePath = this.currentPathSrc ?? "",
                TargetFilePath = this.currentPathDest ?? "",
                TotalFilesToCopy = this.totalFile,
                TotalFilesSize = this.totalSize,
                NbFilesLeftToDo = this.nbFileLeft,
                Progression = this.progress
            };
        }

        /// <summary>
        /// Creates a <see cref="RealTimeState"/> representing a job that is not currently running.
        /// </summary>
        /// <param name="saveName">The name of the backup job.</param>
        /// <returns>A <see cref="RealTimeState"/> object with "INACTIVE" status and zeroed values.</returns>
        public static RealTimeState CreateInactiveState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? "",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "INACTIVE",
                SourceFilePath = "",
                TargetFilePath = "",
                TotalFilesToCopy = 0,
                TotalFilesSize = 0,
                NbFilesLeftToDo = 0,
                Progression = 0
            };
        }

        /// <summary>
        /// Creates a <see cref="RealTimeState"/> representing a successfully completed job.
        /// </summary>
        /// <param name="saveName">The name of the backup job.</param>
        /// <returns>A <see cref="RealTimeState"/> object with "END" status and zeroed values.</returns>
        public static RealTimeState CreateEndState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? "",
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "END",
                SourceFilePath = "",
                TargetFilePath = "",
                TotalFilesToCopy = 0,
                TotalFilesSize = 0,
                NbFilesLeftToDo = 0,
                Progression = 0
            };
        }
    }
}