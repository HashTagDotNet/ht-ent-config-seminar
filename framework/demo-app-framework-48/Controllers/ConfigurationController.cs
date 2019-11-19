using demo_app_framework_48.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace demo_app_framework_48.Controllers
{
    public class ConfigurationController : Controller
    {
        public ConfigurationController()
        {
            
        }
        public ActionResult Index()
        {
            if (!authorizeRequest(this.Request))
            {
                //return Redirect("~/");
                return Redirect(MyAppGlobals.Config.NotAuthorizedUrl);
            }

            var retVal = new Dictionary<string, List<ConfigurationModel>>();

           

            retVal.Add("AppSettings", collectAppSettings());
            retVal.Add("Connection Strings", collectConnectionStrings());
            retVal.Add("Environment Variables", buildSettingsFromEnviroinment());
            retVal.Add("Runtime Context", collectHostSettings());
            retVal.Add("Assembly Info", collectAssemblyInfo());
            return View(retVal);
        }

        private List<ConfigurationModel> collectAssemblyInfo()
        {
            return new List<ConfigurationModel>();
        }

        private List<ConfigurationModel> collectAppSettings()
        {
            var valueList = new List<ConfigurationModel>();
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                valueList.Add(sanitize(new ConfigurationModel(key, ConfigurationManager.AppSettings[key])));
            }
            return valueList.OrderBy(c => c.Key).ToList();
        }

        private List<ConfigurationModel> collectConnectionStrings()
        {
            var valueList = new List<ConfigurationModel>();
            foreach (ConnectionStringSettings cn in ConfigurationManager.ConnectionStrings)
            {
                valueList.Add(sanitize(new ConfigurationModel(cn.Name, cn.ConnectionString)));
            }
            return valueList.OrderBy(c => c.Key).ToList();

        }

        private List<ConfigurationModel> collectHostSettings()
        {
            var retList = new List<ConfigurationModel>();
            retList.Add(new ConfigurationModel("Current Directory", Environment.CurrentDirectory));
            retList.Add(new ConfigurationModel("Command Line", Environment.CommandLine));
            retList.Add(new ConfigurationModel("Thread Id", Environment.CurrentManagedThreadId.ToString()));
            retList.Add(new ConfigurationModel("Logical Drives", string.Join(";", Environment.GetLogicalDrives())));
            retList.Add(new ConfigurationModel("Has Shutdown Started", Environment.HasShutdownStarted.ToString()));
            retList.Add(new ConfigurationModel("Is 64Bit OS", Environment.Is64BitOperatingSystem.ToString()));
            retList.Add(new ConfigurationModel("Is 64Bit Process", Environment.Is64BitProcess.ToString()));
            retList.Add(new ConfigurationModel("Machine Name", Environment.MachineName));
            retList.Add(new ConfigurationModel("OS Version", Environment.OSVersion.ToString()));
            retList.Add(new ConfigurationModel("Processor Count", Environment.ProcessorCount.ToString()));
            retList.Add(new ConfigurationModel("System Directory", Environment.SystemDirectory));
            retList.Add(new ConfigurationModel("Page Size", Environment.SystemPageSize.ToString()));
            retList.Add(new ConfigurationModel("User Domain Name", Environment.UserDomainName));
            retList.Add(new ConfigurationModel("User Name", Environment.UserName));
            retList.Add(new ConfigurationModel("Version", Environment.Version.ToString()));
            retList.Add(new ConfigurationModel("Working Set", Environment.WorkingSet.ToString()));
            return retList.OrderBy(c=>c.Key).ToList();
        }

        private List<ConfigurationModel> buildSettingsFromEnviroinment()
        {
            var retList = new List<ConfigurationModel>();
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                retList.Add(new ConfigurationModel((string)envVar.Key, (string)envVar.Value));
            }
            return retList.OrderBy(c => c.Key).ToList();
        }

        private ConfigurationModel sanitize(ConfigurationModel config)
        {

            var index = config.Value.IndexOf("Password", StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                config.Value = config.Value.Substring(0, index + "Password".Length) + "*******";
            }
            if (string.Compare(config.Key, "Client_Secret", true) == 0)
            {
                config.Value = "********";
            }
            return config;
        }


        private bool authorizeRequest(HttpRequestBase request)
        {
            // check headers, URL parameters, authenticated user, cookies, noonce keys
            return true;

        }
    }
}