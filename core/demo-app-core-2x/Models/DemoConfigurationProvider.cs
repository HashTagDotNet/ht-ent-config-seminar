using HT.Config.Shared.SettingsServiceModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace demo_app_core_2x.Models
{
    public class RemoteConfigOptions
    {
        public int RefreshIntervalMs { get; set; } = 5 * 60 * 1000;
        public string RemoteConfigUrl { get; set; } //= ConfigurationManager.ConnectionStrings["HTConfigService:BaseUrl"].ConnectionString;

        public Action<RemoteConfigContext> OnGetContext { get; set; }
        public Action<Exception> OnError { get; set; }
    }
    public class RemoteConfigContext
    {
        private static string __machineName = Environment.MachineName;

        public string AppName { get; set; }
        //=
        //    ConfigurationManager.AppSettings["ApplicationName"]
        //    ?? ConfigurationManager.AppSettings["AppName"]
        //    ?? ConfigurationManager.AppSettings["HTConfigService:ApplicationName"];

        public string EnvironmentName { get; set; } =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT");
            //?? ConfigurationManager.AppSettings["EnvironmentName"]
            //    ?? ConfigurationManager.AppSettings["HTConfigService:EnvironmentName"];

        public string Override { get; set; } 
            //= ConfigurationManager.AppSettings["Override"]
            //?? ConfigurationManager.AppSettings["HTConfigService:Override"];
        public string ActorName { get; set; } = "Network Service";
        public string ActorHostName { get; set; } = __machineName;
    }
  
    
}
