using demo_app_framework_48.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace demo_app_framework_48
{

    public class MvcApplication : System.Web.HttpApplication
    {

        private static RemoteConfigReader __configReader;
        protected void Application_Start()
        {
            ConfigurationManager.AppSettings.Set("__htconfig:demo:apprecycletoken", Guid.NewGuid().ToString());
            if (__configReader == null)
            {
                __configReader = new RemoteConfigReader(options=>
                {
                    options.RefreshIntervalMs = 5000;
                    options.OnGetContext = getCustomConfigContextProperties;
                }).Start();
            }
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void getCustomConfigContextProperties(RemoteConfigContext remoteContext)
        {
            remoteContext.ActorName = "demo-priv-acct1";
        }
    }
}
