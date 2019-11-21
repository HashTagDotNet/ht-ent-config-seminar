using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared.Admin
{
    public class CreateSettingRequest: RequestBase
    {
        public string AppName { get; set; }
        public string EnvironmentName { get; set; }
        public string Override { get; set; }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}
