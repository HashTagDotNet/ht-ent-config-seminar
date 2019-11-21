using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared.Admin
{
    public class UpdateSettingRequest: CreateSettingRequest
    {
        public string SettingUUID { get; set; }
    }
}
