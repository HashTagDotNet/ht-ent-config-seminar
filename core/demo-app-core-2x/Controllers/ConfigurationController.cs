using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo_app_core_2x.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace demo_app_core_2x.Controllers
{
    public class ConfigurationController : Controller
    {
        private IConfiguration _config;

        public ConfigurationController(IConfiguration config)
        {
            _config = config;
        }
        public IActionResult Index()
        {
            if (!authorizeRequest(this.Request))
            {
                return Redirect("~/");
            }

            var retVal = new Dictionary<string, List<ConfigurationModel>>();
            retVal.Add("Configuration Root", collectConfigRoot(_config));
           // retVal.Add("Environment Variables", buildSettingsFromEnviroinment()); //by default included in IConfiguration
            retVal.Add("Runtime Context", collectHostSettings());

            return View(retVal);
        }

        private List<ConfigurationModel> collectConfigRoot(IConfiguration config)
        {
            return config.AsEnumerable()
                .OrderBy(entry=>entry.Key)
                .ThenBy(entry=>entry.Value)
                .Select(entry => sanitize(new ConfigurationModel(entry.Key, entry.Value)))
                .ToList();            
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
            return retList;
        }

        private List<ConfigurationModel> buildSettingsFromEnviroinment()
        {
            var retList = new List<ConfigurationModel>();
            foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
            {
                retList.Add(new ConfigurationModel((string)envVar.Key, (string)envVar.Value));
            }
            return retList;
        }
        private bool authorizeRequest(HttpRequest request)
        {
            // check headers, URL parameters, authenticated user, cookies, nonce keys
            return true;
        }
        private ConfigurationModel sanitize(ConfigurationModel config)
        {
            if (config.Value == null) return config;
            var index = config.Value.IndexOf("Password", StringComparison.InvariantCultureIgnoreCase);
            if (index >= 0)
            {
                config.Value = config.Value.Substring(0, index + "Password".Length) + "*******";
            }
            if (config.Key.Contains("Client_Secret",StringComparison.InvariantCultureIgnoreCase))
            {
                config.Value = "********";
            }
            return config;
        }


     
    }
}