using HT.Config.Shared.SettingsServiceModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
                                if (setting.Key.StartsWith("ConnectionStrings:", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    var xt = ConfigurationManager.ConnectionStrings.IsReadOnly();
                                }
                                else
                                {
                                    ConfigurationManager.AppSettings.Set(setting.Value.Key, setting.Value.Value);
                                }
                            }
                        }
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
}