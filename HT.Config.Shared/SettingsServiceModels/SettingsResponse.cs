using System;
using System.Collections.Generic;
using System.Text;

namespace HT.Config.Shared.SettingsServiceModels
{
    public class SettingsResponse:ResponseBase
    {
        public SortedDictionary<string, SettingDescription> Settings { get; set; } = new SortedDictionary<string, SettingDescription>();
    }
}
