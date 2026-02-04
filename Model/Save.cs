using System;

namespace easySave_BMT.Model_
{
    public class Save
    {
        public string name { get; set; }
        public string src { get; set; }
        public string dst { get; set; }
        public BackupType backupType { get; set; }
        public State state { get; set; }
        public string lastBackupDate { get; set; }

        public Save() { }

        public Save(string name, string src, string dst, BackupType backupType)
        {
            this.name = name;
            this.src = src;
            this.dst = dst;
            this.backupType = backupType;
            this.state = null;
            this.lastBackupDate = "";
        }
    }
}