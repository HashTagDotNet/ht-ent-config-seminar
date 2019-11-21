using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.ConfigApi.Library.dbModels
{
    public class dbSetting
    {
        public Guid SettingId { get; set; }
        public string AppName { get; set; }
        public string EnvironmentName { get; set; }
        public string Override { get; set; }
        public string SettingKey { get; set; }
        public byte[] SettingValue { get; set; }
    }
}
