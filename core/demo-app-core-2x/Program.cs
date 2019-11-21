using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using demo_app_core_2x.Models;
using HT.Config.Shared.SettingsServiceModels;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace demo_app_core_2x
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, configBuilder) =>
                {
                    var settings = configBuilder.Build();
                    RemoteConfigOptions remoteOptions = new RemoteConfigOptions();
                    settings.Bind("HTConfiguration", remoteOptions);
                    remoteOptions.RemoteConfigUrl = settings["ConnectionStrings:ConfigServiceUrl"];

                    RemoteConfigContext callerContext = new RemoteConfigContext();
                    settings.Bind("HTConfiguration", callerContext);

                    configBuilder.AddHTConfigurationSource(remoteOptions, callerContext);
                })
                .UseStartup<Startup>();
    }

    public static class HTConfigurationExtensions
    {
        public static IConfigurationBuilder AddHTConfigurationSource(this IConfigurationBuilder configBuilder, RemoteConfigOptions options, RemoteConfigContext context)
        {
            configBuilder.Sources.Add(new HTConfigurationSource(options, context));
            return configBuilder;
        }

        public static IDictionary<string, string> Set(this IDictionary<string, string> target, string key, string value)
        {
            if (target.ContainsKey(key))
            {
                target[key] = value;
            }
            else
            {
                target.Add(key, value);
            }
            return target;
        }
    }
    public class HTConfigurationSource : IConfigurationSource
    {
        private RemoteConfigOptions _options;
        private RemoteConfigContext _context;


        public HTConfigurationSource(RemoteConfigOptions options, RemoteConfigContext context)
        {
            this._options = options;
            this._context = context;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HTConfigurationProvider(_options, _context);
        }

    }
    public class HTConfigurationProvider : ConfigurationProvider
    {

        private RemoteConfigOptions _options;
        private RemoteConfigContext _context;
        private Timer _timer;
        public HTConfigurationProvider(RemoteConfigOptions options, RemoteConfigContext context)
        {
            this._options = options;
            this._context = context;
            _timer = new Timer(onLoadConfigurationTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        public override void Load()
        {
            loadConfiguration();
            startTimer();

        }
        private void onLoadConfigurationTimer(object state)
        {
            stopTimer();
            loadConfiguration();
            startTimer();
        }
        public HTConfigurationProvider Start()
        {
            onLoadConfigurationTimer(null);
            return this;
        }
        private void startTimer()
        {
            Data.Set("HTConfiguration:nextloadtime", DateTimeOffset.Now.AddMilliseconds(_options.RefreshIntervalMs).ToString());
            _timer.Change(_options.RefreshIntervalMs, _options.RefreshIntervalMs);
        }
        private void loadConfiguration()
        {
            var ctx = _context;
            try
            {
                _options?.OnGetContext?.Invoke(ctx);

                Data.Set("HTConfiguration:lastloadtime", DateTimeOffset.Now.ToString());
                Data.Set("HTConfiguration:lastloadContext", JsonConvert.SerializeObject(ctx));

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
                                // TODO need to add delete detection here
                                if (Data.ContainsKey(setting.Value.Key))
                                {
                                    Data[setting.Value.Key] = setting.Value.Value;
                                }
                                else
                                {
                                    Data.Add(setting.Value.Key, setting.Value.Value);
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
                Data.Set("HTConfiguration:lastloadResult", "OK");
            }
            catch (Exception ex)
            {
                Data.Set("HTConfiguration:lastloadResult", JsonConvert.SerializeObject(ex, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                }
                ));
                _options.OnError?.Invoke(ex);
            }
        }

        private void stopTimer()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

    }

}
