using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared
{
    public class SettingDescription
    {
        public string SettingUUID { get; set; }

        public string AppName { get; set; }
        public string EnvironmentName { get; set; }
        public string Override { get; set; }
        public string Key { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }
        public int Version { get; set; }

    }
}
