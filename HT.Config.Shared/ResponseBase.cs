using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HT.Config.Shared
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ResponseBase
    {
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

        public string ResponseCode { get; set; }

        public string ResponseMessage { get; set; }

        public bool IsOk => StatusCode < 400;

    }
}
