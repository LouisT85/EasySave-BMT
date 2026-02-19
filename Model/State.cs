using System;

namespace easySave_BMT.Model_
{
    /// <summary>
    /// Represents the runtime execution state of a backup job.
    /// </summary>
    public class State
    {
        /// <summary>
        /// Gets or sets the total file count to process.
        /// </summary>
        public int totalFile { get; set; }

        /// <summary>
        /// Gets or sets the total byte size to process.
        /// </summary>
        public long totalSize { get; set; }

        /// <summary>
        /// Gets or sets the progress percentage.
        /// </summary>
        public int progress { get; set; }

        /// <summary>
        /// Gets or sets the remaining number of files.
        /// </summary>
        public int nbFileLeft { get; set; }

        /// <summary>
        /// Gets or sets the remaining byte size.
        /// </summary>
        public long leftSize { get; set; }

        /// <summary>
        /// Gets or sets the current source file path.
        /// </summary>
        public string currentPathSrc { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current destination file path.
        /// </summary>
        public string currentPathDest { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new empty instance of the <see cref="State"/> class.
        /// </summary>
        public State()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="State"/> class.
        /// </summary>
        /// <param name="totalFile">Total files in the job.</param>
        /// <param name="totalSize">Total size in bytes.</param>
        /// <param name="currentPathSrc">Initial source path.</param>
        /// <param name="currentPathDest">Initial destination path.</param>
        public State(int totalFile, long totalSize, string currentPathSrc, string currentPathDest)
        {
            progress = 0;
            this.totalFile = totalFile;
            this.totalSize = totalSize;
            this.currentPathSrc = currentPathSrc ?? string.Empty;
            this.currentPathDest = currentPathDest ?? string.Empty;
        }

        /// <summary>
        /// Updates execution statistics and current file paths.
        /// </summary>
        public void UpdateState(int progress, int nbFileLeft, long leftSize, string currSrcPath, string currDestPath)
        {
            this.progress = progress;
            this.nbFileLeft = nbFileLeft;
            this.leftSize = leftSize;
            currentPathSrc = currSrcPath ?? string.Empty;
            currentPathDest = currDestPath ?? string.Empty;
        }

        /// <summary>
        /// Converts the current state to a persistent real-time state.
        /// </summary>
        /// <param name="saveName">The backup job name.</param>
        /// <returns>An active <see cref="RealTimeState"/> instance.</returns>
        public RealTimeState ToRealTimeState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? string.Empty,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "ACTIVE",
                SourceFilePath = currentPathSrc,
                TargetFilePath = currentPathDest,
                TotalFilesToCopy = totalFile,
                TotalFilesSize = totalSize,
                NbFilesLeftToDo = nbFileLeft,
                Progression = progress
            };
        }

        /// <summary>
        /// Creates an inactive real-time state for a save.
        /// </summary>
        /// <param name="saveName">The backup job name.</param>
        /// <returns>An inactive <see cref="RealTimeState"/>.</returns>
        public static RealTimeState CreateInactiveState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? string.Empty,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "INACTIVE"
            };
        }

        /// <summary>
        /// Creates an end-state real-time state for a save.
        /// </summary>
        /// <param name="saveName">The backup job name.</param>
        /// <returns>An end-state <see cref="RealTimeState"/>.</returns>
        public static RealTimeState CreateEndState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? string.Empty,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = "END"
            };
        }
    }
}
