using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HT.Config.ConfigApi.Library.Admin;
using HT.Config.Shared.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HT.Config.ConfigApi.Controllers.admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private ISettingAdminService _svc;

        public SettingsController(ISettingAdminService svc)
        {
            _svc = svc;
        }

        [HttpPost]
        public async Task<ActionResult<CreateSettingResponse>> CreateNewSetting([FromBody] CreateSettingRequest request)
        {
            var svcResponse = await _svc.CreateSetting(request);
            return new ObjectResult(svcResponse)
            {
                StatusCode = svcResponse.StatusCode
            };
        }
        [HttpPut("{settingUUID}")]
        public async Task<ActionResult<CreateSettingResponse>> UpdateSetting([FromBody] UpdateSettingRequest request, string settingUUID)
        {
            request.SettingUUID = settingUUID;
            var svcResponse = await _svc.UpdateSetting(request);
            return new ObjectResult(svcResponse)
            {
                StatusCode = svcResponse.StatusCode
            };
        }

        [HttpPut("{settingUUID}/value")]
        public async Task<ActionResult<CreateSettingResponse>> UpdateSettingValue([FromBody] UpdateSettingValueRequest request, string settingUUID)
        {
            request.SettingUUID = settingUUID;
            var svcResponse = await _svc.UpdateSettingValue(request);
            return new ObjectResult(svcResponse)
            {
                StatusCode = svcResponse.StatusCode
            };
        }
        [HttpDelete("{settingUUID}")]
        public async Task<ActionResult<CreateSettingResponse>> DeleteSettingValue([FromBody] DeleteSettingRequest request, string settingUUID)
        {
            request.SettingUUID = settingUUID;
            var svcResponse = await _svc.DeleteSetting(request);
            return new ObjectResult(svcResponse)
            {
                StatusCode = svcResponse.StatusCode
            };
        }
    }
}