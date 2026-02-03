using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using easySave_V1.View_Model;

namespace easySave_BMT.Model_;

{
    class Model 
    {
        // --- Attributes ---
        private string backupsaveSavePath = "./BackupsaveSave.json";
        public List<save> saves { get; set; }

        // Prepare options to indent JSON Files
        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        }; 

        // --- Constructor ---
        public Model()
        {
            // Initalize save List
            this.saves = new List<save>();
        }


        // --- Methods ---
        // Add save
        public int Addsave(string _name, string _src, string _dst, BackupType _backupType)
        {
            try
            {
                // Add save in the program (at the end of the List)
                this.saves.Add(new save(_name, _src, _dst, _backupType));
                Savesaves();

                // Return Success Code
                return 101;
            }
            catch
            {
                // Return Error Code
                return 201;
            }
        }

        // Remove save
        public int Removesave(int _index)
        {
            try
            {
                // Remove save from the program (at index)
                this.saves.RemoveAt(_index);
                Savesaves();

                // Return Success Code
                return 103;
            }
            catch
            {
                // Return Error Code
                return 203;
            }
        }
    }
}