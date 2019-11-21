using HT.Config.ConfigApi.Library.Settings;
using HT.Config.Shared.SettingsServiceModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HT.Config.ConfigApi.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private ISettingsService _svc;

        public SettingsController(ISettingsService service)
        {
            _svc = service;
        }

        [HttpPost]
        public async Task<ActionResult<SettingsResponse>> FindSettings([FromBody] SettingsRequest request)
        {
            var retVal = await _svc.GetSettings(request);
            return new ObjectResult(retVal)
            {
                StatusCode = retVal.StatusCode
            };
        }
    }
}