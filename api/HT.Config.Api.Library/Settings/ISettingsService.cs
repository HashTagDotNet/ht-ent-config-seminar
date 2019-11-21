using HT.Config.Shared.SettingsServiceModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HT.Config.ConfigApi.Library.Settings
{
    public interface ISettingsService
    {
        Task<SettingsResponse> GetSettings(SettingsRequest request);
    }
}
