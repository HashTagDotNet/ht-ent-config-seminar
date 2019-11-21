using HT.Config.Shared.SettingsServiceModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace demo_app_framework_48.Models
{
    public class RemoteConfigOptions
    {
        public int RefreshIntervalMs { get; set; } = 5 * 60 * 1000;
        public string RemoteConfigUrl { get; set; } = ConfigurationManager.ConnectionStrings["HTConfigService:BaseUrl"].ConnectionString;

        public Action<RemoteConfigContext> OnGetContext { get; set; }
        public Action<Exception> OnError { get; set; }
    }

    public class RemoteConfigContext
    {
        private static string __machineName = Environment.MachineName;

        public string AppName { get; set; } =
            ConfigurationManager.AppSettings["ApplicationName"]
            ?? ConfigurationManager.AppSettings["AppName"]
            ?? ConfigurationManager.AppSettings["HTConfigService:ApplicationName"];

        public string EnvironmentName { get; set; } =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT")
            ?? ConfigurationManager.AppSettings["EnvironmentName"]
                ?? ConfigurationManager.AppSettings["HTConfigService:EnvironmentName"];

        public string Override { get; set; } = ConfigurationManager.AppSettings["Override"]
            ?? ConfigurationManager.AppSettings["HTConfigService:Override"];
        public string ActorName { get; set; } = "Network Service";
        public string ActorHostName { get; set; } = __machineName;
    }

    public class RemoteConfigReader
    {
        private Timer _timer;
        private RemoteConfigOptions _options;

        public RemoteConfigReader()
        {
            _timer = new Timer(onLoadConfigurationTimer, null, Timeout.Infinite, Timeout.Infinite);
        }
        public RemoteConfigReader(Action<RemoteConfigOptions> options) : this()
        {
            if (_options == null) _options = new RemoteConfigOptions();
            options?.Invoke(_options);
        }
        public RemoteConfigReader(RemoteConfigOptions options) : this()
        {
            _options = options;
        }

        private void onLoadConfigurationTimer(object state)
        {
            stopTimer();
            loadConfiguration();
            startTimer();
        }
        public RemoteConfigReader Start()
        {
            onLoadConfigurationTimer(null);
            return this;
        }
        private void startTimer()
        {
            ConfigurationManager.AppSettings.Set("__htconfig:nextloadtime", DateTimeOffset.Now.AddMilliseconds(_options.RefreshIntervalMs).ToString());
            _timer.Change(_options.RefreshIntervalMs, _options.RefreshIntervalMs);
        }

        private void loadConfiguration()
        {
            var ctx = new RemoteConfigContext();
            try
            {
                _options?.OnGetContext?.Invoke(ctx);
                ConfigurationManager.AppSettings.Set("__htconfig:lastloadtime", DateTimeOffset.Now.ToString());
                ConfigurationManager.AppSettings.Set("__htconfig:lastloadContext", JsonConvert.SerializeObject(ctx));

                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(_options.RemoteConfigUrl, UriKind.RelativeOrAbsolute);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    SettingsRequest request = new SettingsRequest()
                    {
                        ActorHostName = ctx.ActorHostName,
                        ActorName = ctx.ActorName,
                        AppName = ctx.AppName,
                        EnvironmentName = ctx.EnvironmentName,
                        Override = ctx.Override
                    };
                    var contentToSend = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                    var httpResult = httpClient.PostAsync("api/settings", contentToSend).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (httpResult.IsSuccessStatusCode)
                    {
                        var httpResultString = httpResult.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        SettingsResponse response = JsonConvert.DeserializeObject<SettingsResponse>(httpResultString);
                        if (response.IsOk)
                        {
                            foreach (var setting in response.Settings)
                            {
                                var cnIndex = setting.Key.IndexOf("ConnectionStrings:", StringComparison.InvariantCultureIgnoreCase);

                                if (cnIndex >-1)
                                {
                                    var key = setting.Key.Substring(cnIndex + "ConnectionStrings:".Length); //strip off well known 'ConnectionStrings:' prefix
                                    ConfigurationManager.ConnectionStrings.SetSetting(key, setting.Value.Value, null);
                                }
                                else
                                {
                                    ConfigurationManager.AppSettings.Set(setting.Value.Key, setting.Value.Value);
                                }
                            }
                            // write results to secondary configuration store
                        }
                    }
                    else
                    {
                        // read results from secondary configuration store
                    }
                }
                ConfigurationManager.AppSettings.Set("__htconfig:lastloadResult", "OK");
            }
            catch (Exception ex)
            {
                ConfigurationManager.AppSettings.Set("__htconfig:lastloadResult", JsonConvert.SerializeObject(ex));
                _options.OnError?.Invoke(ex);
            }
        }

        private void stopTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Add new connection string to collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public static void AddSetting(this ConnectionStringSettingsCollection connectionStrings, string name, string connectionString, string providerName)
        {
            connectionStrings.AddSetting(new ConnectionStringSettings(name, connectionString, providerName));
        }

        /// <summary>
        /// Add a new string to collection 
        /// </summary>
        /// <param name="setting"></param>
        public static void AddSetting(this ConnectionStringSettingsCollection connectionStrings, ConnectionStringSettings setting)
        {
            var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            readonlyField.SetValue(connectionStrings, false);
            connectionStrings.Add(setting);
            readonlyField.SetValue(connectionStrings, true);
        }

        /// <summary>
        /// Clear all exsist keys from collection
        /// </summary>
        public static void ClearSettings(this ConnectionStringSettingsCollection connectionStrings)
        {
            var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            readonlyField.SetValue(connectionStrings, false);
            connectionStrings.Clear();
            readonlyField.SetValue(connectionStrings, true);
        }

        public static void SetSetting(this ConnectionStringSettingsCollection connectionStrings, string name, string connectionString, string providerName)
        {
            connectionStrings.SetSetting(new ConnectionStringSettings(name, connectionString, providerName));
        }

        /// <summary>
        /// Update an existing key in the collection
        /// </summary>
        /// <param name="setting"></param>
        public static void SetSetting(this ConnectionStringSettingsCollection connectionStrings, ConnectionStringSettings setting)
        {
            connectionStrings.RemoveSetting(setting.Name);
            connectionStrings.AddSetting(setting);
        }

        /// <summary>
        /// Remove an existing key from collection
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveSetting(this ConnectionStringSettingsCollection connectionStrings, string name)
        {
            var readonlyField = typeof(ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            readonlyField.SetValue(connectionStrings, false);
            connectionStrings.Remove(name);
            readonlyField.SetValue(connectionStrings, true);
        }

    }
}