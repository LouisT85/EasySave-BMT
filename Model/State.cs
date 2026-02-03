namespace easySave_BMT.Model_
{
    public class State
    {
        // --- Attributes ---
        public int totalFile { get; set; }
        public long totalSize { get; set; }
        public int progress { get; set; }
        public int nbFileLeft { get; set; }
        public long leftSize { get; set; }
        public string currentPathSrc { get; set; }
        public string currentPathDest { get; set; }


        // --- Contructors ---
        // Constructor used by Loadsaves()
        public State() { }

        // Constructor used by DoBackup()
        public State(int totalFile, long totalSize, string currentPathSrc, string currentPathDest)
        {
            this.progress = 0;
            this.totalFile = totalFile;
            this.totalSize = totalSize;
            this.currentPathSrc = currentPathSrc;
            this.currentPathDest = currentPathDest;
        }
        public void UpdateState(int progress, int nbFileLeft, long leftSize, string currSrcPath, string currDestPath)
        {
            this.progress = progress;
            this.nbFileLeft = nbFileLeft;
            this.leftSize = leftSize;
            this.currentPathSrc = currSrcPath;
            this.currentPathDest = currDestPath;
        }

    }
}