using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace demo_app_framework_48.Models
{
    public class ConfigurationModel
    {
        public ConfigurationModel(string key, string value)
        {
            Key = key;
            Value = value;
        }
        public string Key { get; set; }

        public string Value { get; set; }
    }
}