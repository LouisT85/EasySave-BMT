using System;
using System.IO;

namespace easySave_BMT.Model_

{
    public class save
    {
  
        public string name { get; set; }
        public string src { get; set; }
        public string dst { get; set; }
        public BackupType backupType { get; set; }
        public State state { get; set; }
        public string lastBackupDate { get; set; }


     
        public save() { }

        // Constructor used by Addsave()
        public save(string _name, string _src, string _dst, BackupType _backupType)
        {
            this.name = _name;
            this.src = _src;
            this.dst = _dst;
            this.backupType = _backupType;
            this.state = null;
        }


        // --- Methods ----
        // Save Log 
        public void SaveLog(DateTime _startDate, string _src, string _dst, long _size, bool isError)
        {
            // Prepare times log
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string startTime = _startDate.ToString("yyyy-MM-dd_HH-mm-ss");
            string elapsedTime = (DateTime.Now - _startDate).ToString();

            if (isError)
            {
                elapsedTime = "-1";
            }

            // Create File if it doesn't exists
            if (!Directory.Exists("./Logs"))
            {
                Directory.CreateDirectory("./Logs");
            }

            // Write log
            File.AppendAllText($"./Logs/{today}.txt", $"{startTime}: {this.name}" +
                $"\nSource: {_src}" +
                $"\nDestination: {_dst}" +
                $"\nSize (Bytes): {_size}" +
                $"\nElapsed Time: {elapsedTime}" +
                "\n\r\n");
        }
    }
}
