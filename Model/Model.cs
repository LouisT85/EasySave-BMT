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
    }
}