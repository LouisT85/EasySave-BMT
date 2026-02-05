namespace easySave_BMT.Model_
{

    public class State
    {
        // --- Attributes ---
        public int totalFile { get; set; }
        
        public long totalSize { get; set; }
        

        /// Progress percentage (0-100)

        public int progress { get; set; }
        

        /// Number of files left to copy

        public int nbFileLeft { get; set; }
        

        /// Size of files left to copy in bytes

        public long leftSize { get; set; }

        /// Current source file path

        public string currentPathSrc { get; set; }
        

        /// Current destination file path

        public string currentPathDest { get; set; }

        // --- Constructors ---

        /// Default constructor for deserialization

        public State() { }


        /// Constructor used by DoBackup()

        public State(int totalFile, long totalSize, string currentPathSrc, string currentPathDest)
        {
            this.progress = 0;
            this.totalFile = totalFile;
            this.totalSize = totalSize;
            this.currentPathSrc = currentPathSrc ?? "";
            this.currentPathDest = currentPathDest ?? "";
        }
        

        /// Updates the state with new values

        public void UpdateState(int progress, int nbFileLeft, long leftSize, string currSrcPath, string currDestPath)
        {
            this.progress = progress;
            this.nbFileLeft = nbFileLeft;
            this.leftSize = leftSize;
            this.currentPathSrc = currSrcPath ?? "";
            this.currentPathDest = currDestPath ?? "";
        }
        

        /// Converts this State to a RealTimeState object

        public RealTimeState ToRealTimeState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? "",
                State = "ACTIVE",
                SourceFilePath = this.currentPathSrc ?? "",
                TargetFilePath = this.currentPathDest ?? "",
                TotalFilesToCopy = this.totalFile,
                TotalFilesSize = this.totalSize,
                NbFilesLeftToDo = this.nbFileLeft,
                Progression = this.progress
            };
        }
        

        /// Creates an END state for a save job

        public static RealTimeState CreateEndState(string saveName)
        {
            return new RealTimeState
            {
                Name = saveName ?? "",
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