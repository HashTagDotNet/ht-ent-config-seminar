using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace demo_app_framework_48.Models
{
    public class MyAppConfig
    {
        /// <summary>
        /// [NoAuthorized] Relative URL where user should be redirected to when they are not authorized (default: '~/')
        /// </summary>
        public string NotAuthorizedUrl 
            => ConfigurationManager.AppSettings["NoAuthorizedUrl"]
            ?? "~/";
        public string MyDatabaseConnectionString 
            => ConfigurationManager.ConnectionStrings["db"].ConnectionString;
    }

    public class MyAppGlobals
    {
        public static MyAppConfig Config { get; set; }
            = new MyAppConfig();
    }


}