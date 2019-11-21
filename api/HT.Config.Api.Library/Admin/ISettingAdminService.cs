using HT.Config.Shared.Admin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HT.Config.ConfigApi.Library.Admin
{
    public interface ISettingAdminService
    {
        Task<CreateSettingResponse> CreateSetting(CreateSettingRequest request);
        Task<UpdateSettingResponse> UpdateSetting(UpdateSettingRequest request);
        Task<UpdateSettingValueResponse> UpdateSettingValue(UpdateSettingValueRequest request);
        Task<DeleteSettingResponse> DeleteSetting(DeleteSettingRequest request);
    }
}
