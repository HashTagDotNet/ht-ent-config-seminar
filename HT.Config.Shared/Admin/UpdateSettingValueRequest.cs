using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared.Admin
{
   public class UpdateSettingValueRequest:RequestBase
    {
        public string SettingUUID { get; set; }
        public string Value { get; set; }
    }
}
