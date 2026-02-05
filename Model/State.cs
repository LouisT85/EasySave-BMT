namespace easySave_BMT.Model_
{
    public class State
    {
        public int totalFile { get; set; }
        public long totalSize { get; set; }
        public int progress { get; set; }
        public int nbFileLeft { get; set; }
        public long leftSize { get; set; }
        public string currentPathSrc { get; set; }
        public string currentPathDest { get; set; }

        public State() { }

        public State(int totalFile, long totalSize, string currentPathSrc, string currentPathDest)
        {
            this.progress = 0;
            this.totalFile = totalFile;
            this.totalSize = totalSize;
            this.currentPathSrc = currentPathSrc ?? "";
            this.currentPathDest = currentPathDest ?? "";
        }

        public void UpdateState(int progress, int nbFileLeft, long leftSize, string currSrcPath, string currDestPath)
        {
            this.progress = progress;
            this.nbFileLeft = nbFileLeft;
            this.leftSize = leftSize;
            this.currentPathSrc = currSrcPath ?? "";
            this.currentPathDest = currDestPath ?? "";
        }

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
