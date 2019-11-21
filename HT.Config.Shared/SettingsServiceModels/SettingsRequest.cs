using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared.SettingsServiceModels
{
    public class SettingsRequest: RequestBase
    {
        public string AppName { get; set; }
        public string EnvironmentName { get; set; }
        public string Override { get; set; }
       


    }
}
