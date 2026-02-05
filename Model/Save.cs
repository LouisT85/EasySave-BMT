using System;

namespace easySave_BMT.Model_
{

    /// Represents a backup job configuration

    public class Save
    {

        /// Name of the backup job (1-20 characters)

        public string name { get; set; }
        

        /// Source directory path

        public string src { get; set; }
        

        /// Destination directory path

        public string dst { get; set; }
        

        /// Type of backup (FULL or DIFFERENTIAL)

        public BackupType backupType { get; set; }
        

        /// Current state of the backup (null when not running)

        public State state { get; set; }
        

        /// Last backup execution date

        public string lastBackupDate { get; set; }


        /// Default constructor for JSON deserialization

        public Save() 
        {
            this.state = null;
            this.lastBackupDate = "";
        }

        /// Constructor used by Addsave()
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